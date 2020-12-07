using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Attribulator.API.Data;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.DB;
using VaultLib.Core.Hashing;
using VaultLib.Core.Types;

namespace Attribulator.API.Serialization
{
    /// <summary>
    ///     Base class for storage formats to inherit from.
    /// </summary>
    public abstract class BaseStorageFormat : IDatabaseStorageFormat
    {
        private static readonly Dictionary<(string, string), VltClassField> FieldCache =
            new Dictionary<(string, string), VltClassField>();

        /// <inheritdoc />
        public abstract SerializedDatabaseInfo LoadInfo(string sourceDirectory);

        /// <inheritdoc />
        public async Task<IEnumerable<LoadedFile>> DeserializeAsync(string sourceDirectory,
            Database destinationDatabase, IEnumerable<string> fileNames = null)
        {
            var loadedFiles = new List<LoadedFile>();
            var loadedDatabase = LoadInfo(sourceDirectory);
            var fileNameList = fileNames?.ToList() ?? new List<string>();

            if (string.IsNullOrEmpty(loadedDatabase.PrimaryVaultName))
                throw new Exception("No primary vault name has been specified.");

            foreach (var loadedDatabaseClass in loadedDatabase.Classes)
            {
                var vltClass = new VltClass(loadedDatabaseClass.Name);

                foreach (var loadedDatabaseClassField in loadedDatabaseClass.Fields)
                {
                    var field = new VltClassField(
                        destinationDatabase.Options.Type == DatabaseType.X86Database
                            ? VLT32Hasher.Hash(loadedDatabaseClassField.Name)
                            : VLT64Hasher.Hash(loadedDatabaseClassField.Name),
                        loadedDatabaseClassField.Name,
                        loadedDatabaseClassField.TypeName,
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

                    FieldCache[(vltClass.Name, field.Name)] = field;
                }

                destinationDatabase.AddClass(vltClass);
            }

            foreach (var loadedDatabaseType in loadedDatabase.Types)
                destinationDatabase.Types.Add(new DatabaseTypeInfo
                    {Name = loadedDatabaseType.Name, Size = loadedDatabaseType.Size});


            var collectionParentDictionary = new Dictionary<string, string>();
            var collectionDictionary = new Dictionary<string, VltCollection>();
            var vaultsToSaveDictionary = new Dictionary<string, List<Vault>>();
            var tempCollectionListsDictionary = new Dictionary<string, List<VltCollection>>();
            var seenCollections = new Dictionary<string, bool>();

            void AddCollectionsToList(Vault newVault, VltClass vltClass, string vaultDirectory,
                ICollection<VltCollection> collectionList,
                IEnumerable<SerializedCollection> collectionsToAdd)
            {
                if (collectionList == null)
                    throw new Exception("collectionList should not be null!");
                collectionsToAdd ??= new List<SerializedCollection>();

                foreach (var loadedCollection in collectionsToAdd)
                {
                    var newVltCollection = new VltCollection(newVault, vltClass, loadedCollection.Name);

                    if (!seenCollections.TryAdd(newVltCollection.ShortPath, true))
                        throw new Exception("Duplicate collection detected: " + newVltCollection.ShortPath);

                    foreach (var (key, value) in loadedCollection.Data)
                    {
                        if (!FieldCache.TryGetValue((vltClass.Name, key), out var field))
                            throw new Exception(
                                $"Cannot find field: {vltClass.Name}/{key}");

                        newVltCollection.SetRawValue(key,
                            ConvertSerializedValueToDataValue(destinationDatabase,
                                destinationDatabase.Options.GameId, vaultDirectory,
                                vltClass, field,
                                newVltCollection, value));
                    }

                    collectionParentDictionary[newVltCollection.ShortPath] =
                        loadedCollection.ParentName;
                    collectionList.Add(newVltCollection);
                    collectionDictionary[newVltCollection.ShortPath] = newVltCollection;
                }
            }

            foreach (var file in loadedDatabase.Files.Where(f => fileNames == null || fileNameList.Contains(f.Name)))
            {
                var baseDirectory = Path.Combine(sourceDirectory, file.Group, file.Name);
                vaultsToSaveDictionary[file.Name] = new List<Vault>();

                foreach (var vaultName in file.Vaults)
                {
                    var vaultDirectory = Path.Combine(baseDirectory, vaultName).Trim();
                    var newVault = new Vault(vaultName)
                        {Database = destinationDatabase, IsPrimaryVault = vaultName == loadedDatabase.PrimaryVaultName};
                    if (Directory.Exists(vaultDirectory))
                    {
                        var collectionsToBeAdded = new List<VltCollection>();

                        foreach (var dataFilePath in GetDataFilePaths(vaultDirectory))
                        {
                            var className = Path.GetFileNameWithoutExtension(dataFilePath);
                            var vltClass = destinationDatabase.FindClass(className);

                            if (vltClass == null)
                                throw new InvalidDataException($"Unknown class: {className} ({dataFilePath})");

                            try
                            {
                                var collections = (await LoadDataFileAsync(dataFilePath)).ToList();
                                var newCollections = new List<VltCollection>();
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
                        tempCollectionListsDictionary[vaultName] = new List<VltCollection>();
                    }

                    vaultsToSaveDictionary[file.Name].Add(newVault);
                    destinationDatabase.Vaults.Add(newVault);
                }

                loadedFiles.Add(new LoadedFile(file.Name, file.Group, vaultsToSaveDictionary[file.Name]));
            }


            var resolved = new List<VaultDependencyNode>();
            var unresolved = new List<VaultDependencyNode>();

            foreach (var vault in destinationDatabase.Vaults)
            {
                var vaultCollections = tempCollectionListsDictionary[vault.Name];
                var node = new VaultDependencyNode(vault);

                foreach (var parentCollection in from vaultCollection in vaultCollections
                    let parentKey = collectionParentDictionary[vaultCollection.ShortPath]
                    where !string.IsNullOrEmpty(parentKey)
                    select collectionDictionary[$"{vaultCollection.Class.Name}/{parentKey}"]
                    into parentCollection
                    where parentCollection.Vault.Name != vault.Name
                    select parentCollection)
                    node.AddEdge(new VaultDependencyNode(parentCollection.Vault));

                ResolveDependencies(node, resolved, unresolved);

                Debug.WriteLine("Vault {0}: {1} collections", vault.Name, vaultCollections.Count);
            }

            resolved = resolved.Distinct(VaultDependencyNode.VaultComparer).ToList();
            unresolved = unresolved.Distinct(VaultDependencyNode.VaultComparer).ToList();

            if (unresolved.Count != 0) throw new Exception("Cannot continue loading - unresolved vault dependencies");

            foreach (var node in resolved)
            {
                var vault = node.Vault;
                var vaultCollections = tempCollectionListsDictionary[vault.Name];

                Debug.WriteLine("Loading collections for vault {0} ({1})", vault.Name, vaultCollections.Count);

                foreach (var collection in vaultCollections)
                {
                    var parentKey = collectionParentDictionary[collection.ShortPath];

                    if (string.IsNullOrEmpty(parentKey))
                    {
                        // Add collection directly
                        destinationDatabase.RowManager.AddCollection(collection);
                    }
                    else
                    {
                        var parentCollection = collectionDictionary[$"{collection.Class.Name}/{parentKey}"];
                        parentCollection.AddChild(collection);
                    }
                }
            }

            return loadedFiles;
        }

        /// <inheritdoc />
        public abstract void Serialize(Database sourceDatabase, string destinationDirectory,
            IEnumerable<LoadedFile> loadedFiles);

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

        protected abstract Task<IEnumerable<SerializedCollection>> LoadDataFileAsync(string path);

        // TODO: rework value deserialization
        protected abstract VLTBaseType ConvertSerializedValueToDataValue(Database database, string gameId, string dir,
            VltClass vltClass,
            VltClassField field,
            VltCollection vltCollection, object serializedValue, bool createInstance = true);


        private static void ResolveDependencies(VaultDependencyNode node, ICollection<VaultDependencyNode> resolved,
            ICollection<VaultDependencyNode> unresolved)
        {
            unresolved.Add(node);

            foreach (var edge in node.Edges.Where(edge => !resolved.Contains(edge)))
                ResolveDependencies(edge, resolved, unresolved);

            resolved.Add(node);
            unresolved.Remove(node);
        }

        private class VaultDependencyNode
        {
            public VaultDependencyNode(Vault vault)
            {
                Vault = vault;
                Edges = new List<VaultDependencyNode>();
            }

            public static IEqualityComparer<VaultDependencyNode> VaultComparer { get; } = new VaultEqualityComparer();

            public List<VaultDependencyNode> Edges { get; }
            public Vault Vault { get; }

            public void AddEdge(VaultDependencyNode node)
            {
                Edges.Add(node);
            }

            private sealed class VaultEqualityComparer : IEqualityComparer<VaultDependencyNode>
            {
                public bool Equals(VaultDependencyNode x, VaultDependencyNode y)
                {
                    if (ReferenceEquals(x, y)) return true;
                    if (ReferenceEquals(x, null)) return false;
                    if (ReferenceEquals(y, null)) return false;
                    if (x.GetType() != y.GetType()) return false;
                    return x.Vault.Name == y.Vault.Name;
                }

                public int GetHashCode(VaultDependencyNode obj)
                {
                    return obj.Vault != null ? obj.Vault.GetHashCode() : 0;
                }
            }
        }
    }
}