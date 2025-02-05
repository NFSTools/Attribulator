using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Attribulator.API.Data;
using Attribulator.API.Utils;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.DB;
using VaultLib.Core.Hashing;
using VaultLib.Core.Types;

#nullable enable
namespace Attribulator.API.Serialization
{
    /// <summary>
    ///     Base class for storage formats to inherit from.
    /// </summary>
    public abstract class BaseStorageFormat : IDatabaseStorageFormat
    {
        // private static readonly Dictionary<(string, string), VltClassField> FieldCache =
        //     new Dictionary<(string, string), VltClassField>();

        /// <inheritdoc />
        public abstract SerializedDatabaseInfo LoadInfo<TKey>(string sourceDirectory,
            Database<TKey> destinationDatabase) where TKey : struct, IKey<TKey>;

        /// <inheritdoc />
        public async Task<IEnumerable<LoadedFile<TKey>>> DeserializeAsync<TKey>(string sourceDirectory,
            Database<TKey> destinationDatabase, IEnumerable<string> fileNames = null) where TKey : struct, IKey<TKey>
        {
            var loadedFiles = new List<LoadedFile<TKey>>();
            SerializedDatabaseInfo loadedDatabase;

            try
            {
                loadedDatabase = LoadInfo(sourceDirectory, destinationDatabase);
            }
            catch (Exception e)
            {
                throw new Exception("Error while loading database info", e);
            }

            var fileNameList = fileNames?.ToList() ?? new List<string>();

            if (string.IsNullOrEmpty(loadedDatabase.PrimaryVaultName))
                throw new Exception("No primary vault name has been specified.");

            var fieldCache = new Dictionary<VltUtils.FieldIdentifier<TKey>, VltClassField<TKey>>();

            foreach (var loadedDatabaseClass in loadedDatabase.Classes)
            {
                var classKey = KeyUtils.StringToKey<TKey>(loadedDatabaseClass.Name, true);
                var vltClass = new VltClass<TKey>(classKey)
                {
                    StaticSize = loadedDatabaseClass.StaticSize,
                    LayoutSize = loadedDatabaseClass.LayoutSize,
                };

                foreach (var loadedDatabaseClassField in loadedDatabaseClass.Fields)
                {
                    var fieldKey = KeyUtils.StringToKey<TKey>(loadedDatabaseClassField.Name, true);
                    var typeKey = KeyUtils.StringToKey<TKey>(loadedDatabaseClassField.TypeName, true);
                    var field = new VltClassField<TKey>(
                        vltClass,
                        fieldKey,
                        typeKey,
                        loadedDatabaseClassField.Flags,
                        loadedDatabaseClassField.Alignment,
                        loadedDatabaseClassField.Size,
                        loadedDatabaseClassField.MaxCount,
                        loadedDatabaseClassField.Offset);
                    // Handle static value
                    if (loadedDatabaseClassField.StaticValue != null)
                        field.StaticValue = ConvertSerializedValueToDataValue(destinationDatabase,
                            destinationDatabase.Options.GameId, sourceDirectory,
                            vltClass, field, null,
                            loadedDatabaseClassField.StaticValue);

                    vltClass.Fields.Add(field.Key, field);

                    fieldCache.Add(VltUtils.CreateFieldIdentifier(vltClass, field), field);
                    // fieldCache[(loadedDatabaseClass.Name, loadedDatabaseClassField.Name)] = field;
                }

                destinationDatabase.AddClass(vltClass);
            }

            foreach (var loadedDatabaseType in loadedDatabase.Types)
                destinationDatabase.Types.Add(new DatabaseTypeInfo
                    { Name = loadedDatabaseType.Name, Size = loadedDatabaseType.Size });


            var collectionParentDictionary =
                new Dictionary<VltUtils.CollectionIdentifier<TKey>, VltUtils.CollectionIdentifier<TKey>?>();
            var collectionDictionary = new Dictionary<VltUtils.CollectionIdentifier<TKey>, VltCollection<TKey>>();
            var vaultsToSaveDictionary = new Dictionary<string, List<Vault<TKey>>>();
            var tempCollectionListsDictionary = new Dictionary<string, List<VltCollection<TKey>>>();
            var seenCollections = new HashSet<VltUtils.CollectionIdentifier<TKey>>();

            void AddCollectionsToList(Vault<TKey> newVault, VltClass<TKey> vltClass, string vaultDirectory,
                ICollection<VltCollection<TKey>> collectionList,
                IEnumerable<SerializedCollection<TKey>> collectionsToAdd)
            {
                if (collectionList == null)
                    throw new Exception("collectionList should not be null!");
                collectionsToAdd ??= new List<SerializedCollection<TKey>>();

                foreach (var loadedCollection in collectionsToAdd)
                {
                    var collectionKey = KeyUtils.StringToKey<TKey>(loadedCollection.Name, true);
                    var newCollection =
                        new VltCollection<TKey>(newVault, vltClass, collectionKey);
                    var newCollectionIdentifier = VltUtils.CreateCollectionIdentifier(newCollection);
                    if (!seenCollections.Add(newCollectionIdentifier))
                        throw new Exception("Duplicate collection detected");

                    foreach (var entry in loadedCollection.Data.GetEntries())
                    {
                        var key = entry.Key;
                        var value = entry.Value;
                        if (!fieldCache.TryGetValue(VltUtils.CreateFieldIdentifier(vltClass, key), out var field))
                            throw new Exception(
                                $"Cannot find field: {vltClass.Key}/{key}");

                        newCollection.SetRawValue(key,
                            ConvertSerializedValueToDataValue(destinationDatabase,
                                destinationDatabase.Options.GameId, vaultDirectory,
                                vltClass, field,
                                newCollection, value));
                    }

                    VltUtils.CollectionIdentifier<TKey>? parentCollectionId;

                    if (string.IsNullOrEmpty(loadedCollection.ParentName))
                    {
                        parentCollectionId = null;
                    }
                    else
                    {
                        parentCollectionId = new VltUtils.CollectionIdentifier<TKey>(vltClass.Key,
                            KeyUtils.StringToKey<TKey>(loadedCollection.ParentName, true));
                    }

                    collectionParentDictionary[newCollectionIdentifier] = parentCollectionId;
                    collectionList.Add(newCollection);
                    collectionDictionary[newCollectionIdentifier] = newCollection;
                }
            }

            foreach (var file in loadedDatabase.Files.Where(f => fileNames == null || fileNameList.Contains(f.Name)))
            {
                var baseDirectory = Path.Combine(sourceDirectory, file.Group, file.Name);
                vaultsToSaveDictionary[file.Name] = new List<Vault<TKey>>();

                foreach (var vault in file.Vaults)
                {
                    var vaultName = vault.Name;
                    var vaultDirectory = Path.Combine(baseDirectory, vaultName).Trim();
                    var newVault = new Vault<TKey>(destinationDatabase, vaultName)
                    {
                        IsPrimaryVault = vaultName == loadedDatabase.PrimaryVaultName,
                        Version = vault.Version
                    };
                    if (Directory.Exists(vaultDirectory))
                    {
                        var collectionsToBeAdded = new List<VltCollection<TKey>>();

                        foreach (var dataFilePath in GetDataFilePaths(vaultDirectory))
                        {
                            var className = Path.GetFileNameWithoutExtension(dataFilePath);
                            var vltClass = destinationDatabase.FindClass(className);

                            if (vltClass == null)
                                throw new InvalidDataException($"Unknown class: {className} ({dataFilePath})");

                            try
                            {
                                var collections = (await LoadDataFileAsync(dataFilePath, destinationDatabase, vltClass))
                                    .ToList();
                                var newCollections = new List<VltCollection<TKey>>();
                                AddCollectionsToList(newVault, vltClass, vaultDirectory, newCollections, collections);

                                collectionsToBeAdded.AddRange(newCollections);
                            }
                            catch (Exception e)
                            {
                                throw new InvalidDataException($"Error when loading file {dataFilePath}", e);
                            }
                        }

                        tempCollectionListsDictionary[newVault.Name] = collectionsToBeAdded;
                    }
                    else
                    {
                        Console.WriteLine("WARN: vault {0} has no folder; looked for {1}", vaultName, vaultDirectory);
                        tempCollectionListsDictionary[vaultName] = new List<VltCollection<TKey>>();
                    }

                    vaultsToSaveDictionary[file.Name].Add(newVault);
                    destinationDatabase.Vaults.Add(newVault);
                }

                loadedFiles.Add(new LoadedFile<TKey>(file.Name, file.Group, vaultsToSaveDictionary[file.Name]));
            }


            var resolved = new List<VaultDependencyNode<TKey>>();
            var unresolved = new List<VaultDependencyNode<TKey>>();

            foreach (var vault in destinationDatabase.Vaults)
            {
                var vaultCollections = tempCollectionListsDictionary[vault.Name];
                var node = new VaultDependencyNode<TKey>(vault);

                foreach (var parentCollection in from vaultCollection in vaultCollections
                         let parentKey =
                             collectionParentDictionary[VltUtils.CreateCollectionIdentifier(vaultCollection)]
                         where parentKey != null
                         select collectionDictionary[parentKey]
                         into parentCollection
                         where parentCollection.Vault.Name != vault.Name
                         select parentCollection)
                    node.AddEdge(new VaultDependencyNode<TKey>(parentCollection.Vault));

                ResolveDependencies(node, resolved, unresolved);

                Debug.WriteLine("Vault {0}: {1} collections", vault.Name, vaultCollections.Count);
            }

            resolved = resolved.Distinct(VaultDependencyNode<TKey>.VaultComparer).ToList();
            unresolved = unresolved.Distinct(VaultDependencyNode<TKey>.VaultComparer).ToList();

            if (unresolved.Count != 0) throw new Exception("Cannot continue loading - unresolved vault dependencies");

            foreach (var node in resolved)
            {
                var vault = node.Vault;
                var vaultCollections = tempCollectionListsDictionary[vault.Name];

                Debug.WriteLine("Loading collections for vault {0} ({1})", vault.Name, vaultCollections.Count);

                foreach (var collection in vaultCollections)
                {
                    var parentKey = collectionParentDictionary[VltUtils.CreateCollectionIdentifier(collection)];

                    destinationDatabase.RowManager.AddCollection(collection);

                    if (parentKey is null) continue;
                    var parentCollection = collectionDictionary[parentKey];
                    parentCollection.AddChild(collection);
                }
            }

            return loadedFiles;
        }

        /// <inheritdoc />
        public abstract void Serialize<TKey>(Database<TKey> sourceDatabase, string destinationDirectory,
            IEnumerable<LoadedFile<TKey>> loadedFiles, Func<Vault<TKey>, bool> filterFunc = null)
            where TKey : struct, IKey<TKey>;

        public abstract void Backup<TKey>(string srcDirectory, string destinationDirectory,
            LoadedFile<TKey> file,
            IEnumerable<Vault<TKey>> vaults) where TKey : struct, IKey<TKey>;

        /// <inheritdoc />
        public abstract string GetFormatId();

        /// <inheritdoc />
        public abstract string GetFormatName();

        /// <inheritdoc />
        public abstract bool CanDeserializeFrom(string sourceDirectory);

        /// <inheritdoc />
        public virtual async ValueTask<string> ComputeHashAsync(string sourceDirectory,
            SerializedDatabaseFile loadedFile)
        {
            var path = Path.Combine(sourceDirectory, loadedFile.Group, loadedFile.Name);

            // assuming you want to include nested folders
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                .OrderBy(p => p).ToList();

            if (files.Count <= 0) return string.Empty;
            var md5 = MD5.Create();

            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];

                // hash path
                var relativePath = file.Substring(path.Length + 1);
                var pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLower());
                md5.TransformBlock(pathBytes, 0, pathBytes.Length, pathBytes, 0);

                // hash contents
                var contentBytes = await File.ReadAllBytesAsync(file);
                if (i == files.Count - 1)
                    md5.TransformFinalBlock(contentBytes, 0, contentBytes.Length);
                else
                    md5.TransformBlock(contentBytes, 0, contentBytes.Length, contentBytes, 0);
            }

            return BitConverter.ToString(md5.Hash).Replace("-", "").ToLower();
        }

        protected abstract IEnumerable<string> GetDataFilePaths(string directory);

        protected abstract Task<IEnumerable<SerializedCollection<TKey>>> LoadDataFileAsync<TKey>(string path,
            Database<TKey> database,
            VltClass<TKey> vltClass) where TKey : struct, IKey<TKey>;

        // TODO: rework value deserialization
        protected abstract object ConvertSerializedValueToDataValue<TKey>(Database<TKey> database, string gameId,
            string dir,
            VltClass<TKey> vltClass,
            VltClassField<TKey> field,
            VltCollection<TKey> vltCollection, object serializedValue, bool createInstance = true)
            where TKey : struct, IKey<TKey>;

        private static void ResolveDependencies<TKey>(VaultDependencyNode<TKey> node,
            ICollection<VaultDependencyNode<TKey>> resolved,
            ICollection<VaultDependencyNode<TKey>> unresolved) where TKey : struct, IKey<TKey>
        {
            unresolved.Add(node);

            foreach (var edge in node.Edges.Where(edge => !resolved.Contains(edge)))
                ResolveDependencies(edge, resolved, unresolved);

            resolved.Add(node);
            unresolved.Remove(node);
        }

        private class VaultDependencyNode<TKey> where TKey : struct, IKey<TKey>
        {
            public VaultDependencyNode(Vault<TKey> vault)
            {
                Vault = vault;
                Edges = new List<VaultDependencyNode<TKey>>();
            }

            public static IEqualityComparer<VaultDependencyNode<TKey>> VaultComparer { get; } =
                new VaultEqualityComparer();

            public List<VaultDependencyNode<TKey>> Edges { get; }
            public Vault<TKey> Vault { get; }

            public void AddEdge(VaultDependencyNode<TKey> node)
            {
                Edges.Add(node);
            }

            private sealed class VaultEqualityComparer : IEqualityComparer<VaultDependencyNode<TKey>>
            {
                public bool Equals(VaultDependencyNode<TKey> x, VaultDependencyNode<TKey> y)
                {
                    if (ReferenceEquals(x, y)) return true;
                    if (ReferenceEquals(x, null)) return false;
                    if (ReferenceEquals(y, null)) return false;
                    if (x.GetType() != y.GetType()) return false;
                    return x.Vault.Name == y.Vault.Name;
                }

                public int GetHashCode(VaultDependencyNode<TKey> obj)
                {
                    return obj.Vault != null ? obj.Vault.GetHashCode() : 0;
                }
            }
        }
    }
}