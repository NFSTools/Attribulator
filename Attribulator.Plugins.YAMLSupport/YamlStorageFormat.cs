using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Attribulator.API.Data;
using Attribulator.API.Serialization;
using Attribulator.Plugins.YAMLSupport.Helpers;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.DB;
using VaultLib.Core.Types;
using VaultLib.Core.Types.Attrib;
using VaultLib.Core.Utils;
using YamlDotNet.Serialization;

namespace Attribulator.Plugins.YAMLSupport
{
    /// <summary>
    ///     Implements the YAML storage format.
    /// </summary>
    public class YamlStorageFormat : BaseStorageFormat
    {
        public override SerializedDatabaseInfo LoadInfo(string sourceDirectory, Database destinationDatabase)
        {
            using var dbs = new StreamReader(Path.Combine(sourceDirectory, "info.yml"));

            var deserializer = new DeserializerBuilder().Build();
            var serializer = new SerializerBuilder().Build();

            // Insane strategy to get proper static values:
            // 1. Read the schema with StaticValue as an object. Complex types turn into dictionaries.
            // 2. Once we know the type of every field (after reading the schema the first time), re-serialize all the
            //    static values and deserialize them AGAIN, this time with their proper types. (Except for the special cases.)
            // 3. Replace the original StaticValues with the new, (almost) properly typed ones.
            // 4. Profit.
            var serializedDatabaseInfo = deserializer.Deserialize<SerializedDatabaseInfo>(dbs);

            foreach (var serializedDatabaseClass in serializedDatabaseInfo.Classes)
            {
                foreach (var serializedDatabaseClassField in serializedDatabaseClass.Fields)
                {
                    if ((serializedDatabaseClassField.Flags & DefinitionFlags.IsStatic) == 0)
                    {
                        continue;
                    }

                    var fieldUnderlyingType =
                        destinationDatabase.TypeRegistry.ResolveType(serializedDatabaseClassField.TypeName);

                    var effectiveFieldUnderlyingType = CloakingHelper.IsTypeAStringInDisguise(fieldUnderlyingType)
                        ? typeof(string)
                        : fieldUnderlyingType;

                    var staticType = (serializedDatabaseClassField.Flags & DefinitionFlags.Array) != 0
                        ? typeof(CustomSerializedArray<>).MakeGenericType(fieldUnderlyingType)
                        : effectiveFieldUnderlyingType;

                    var serializedStaticValue = serializer.Serialize(serializedDatabaseClassField.StaticValue);

                    serializedDatabaseClassField.StaticValue =
                        deserializer.Deserialize(serializedStaticValue, staticType);
                }
            }

            return serializedDatabaseInfo;
        }

        public override void Serialize(Database sourceDatabase, string destinationDirectory,
            IEnumerable<LoadedFile> loadedFiles, Func<Vault, bool> filterFunc = null)
        {
            filterFunc ??= _ => true;

            var loadedFileList = loadedFiles.ToList();
            var serializedDatabaseInfo = new SerializedDatabaseInfo
            {
                Classes = new List<SerializedDatabaseClass>(),
                Files = new List<SerializedDatabaseFile>(),
                Types = new List<SerializedTypeInfo>(),
                PrimaryVaultName = sourceDatabase.Vaults.First(v => v.IsPrimaryVault).Name
            };

            serializedDatabaseInfo.Files.AddRange(loadedFileList.Select(f => new SerializedDatabaseFile
                { Name = f.Name, Group = f.Group, Vaults = f.Vaults.Select(v => v.Name).ToList() }));

            foreach (var databaseType in sourceDatabase.Types)
            {
                serializedDatabaseInfo.Types.Add(new SerializedTypeInfo
                {
                    Name = databaseType.Name,
                    Size = databaseType.Size
                });
            }

            foreach (var databaseClass in sourceDatabase.Classes)
            {
                var serializedDatabaseClass = new SerializedDatabaseClass
                {
                    Name = databaseClass.Name,
                    StaticSize = databaseClass.StaticSize,
                    Fields = new List<SerializedDatabaseClassField>(),
                };

                serializedDatabaseClass.Fields.AddRange(databaseClass.Fields.Values.Select(field =>
                    new SerializedDatabaseClassField
                    {
                        Name = field.Name,
                        TypeName = field.TypeName,
                        Alignment = field.Alignment,
                        Flags = field.Flags,
                        MaxCount = field.MaxCount,
                        Size = field.Size,
                        Offset = field.Offset,
                        StaticValue =
                            ConvertVltValueToSerializedValue(destinationDirectory, null, field, field.StaticValue)
                    }));

                serializedDatabaseInfo.Classes.Add(serializedDatabaseClass);
            }

            var infoSerializer = new SerializerBuilder().WithQuotingNecessaryStrings(true).Build();

            using var sw = new StreamWriter(Path.Combine(destinationDirectory, "info.yml"));
            infoSerializer.Serialize(sw, serializedDatabaseInfo);

            var classSpecificSerializers = sourceDatabase.Classes.ToDictionary(c => c.Name, c =>
            {
                return new SerializerBuilder()
                    .WithQuotingNecessaryStrings(true)
                    .DisableAliases()
                    .WithTypeInspector(
                        inspector => new VltClassSchemaTypeInspector(inspector, sourceDatabase, c))
                    // add YamlIgnore to some annoying matrix properties
                    .WithAttributeOverride<Matrix4x4>(m => m.Translation, new YamlIgnoreAttribute())
                    .WithAttributeOverride<Matrix4x4>(m => m.IsIdentity, new YamlIgnoreAttribute())
                    .Build();
            });

            foreach (var loadedDatabaseFile in loadedFileList)
            {
                var baseDirectory =
                    Path.Combine(destinationDirectory, loadedDatabaseFile.Group, loadedDatabaseFile.Name);
                Directory.CreateDirectory(baseDirectory);

                foreach (var vault in loadedDatabaseFile.Vaults.Where(filterFunc))
                {
                    var vaultDirectory = Path.Combine(baseDirectory, vault.Name).Trim();
                    Directory.CreateDirectory(vaultDirectory);

                    // Problem: Gameplay data is separated into numerous vaults, so we can't easily construct a proper hierarchy
                    // Solution: Store the name of the parent node instead of having an array of children.

                    foreach (var collectionGroup in sourceDatabase.RowManager.GetCollectionsInVault(vault)
                                 .GroupBy(v => v.Class.Name))
                    {
                        var serializedCollections = new List<CustomSerializedCollection>();
                        ConvertVltCollectionsToSerializedCollections(vaultDirectory, collectionGroup,
                            serializedCollections);

                        using var vw = new StreamWriter(Path.Combine(vaultDirectory, collectionGroup.Key + ".yml"));
                        classSpecificSerializers[collectionGroup.Key].Serialize(vw, serializedCollections);
                    }
                }
            }
        }

        public override void Backup(string srcDirectory, string destinationDirectory,
            LoadedFile file,
            IEnumerable<Vault> vaults)
        {
            var srcFileBaseDir =
                Path.Combine(srcDirectory, file.Group, file.Name);
            var destinationFileBaseDir =
                Path.Combine(destinationDirectory, file.Group, file.Name);
            Directory.CreateDirectory(destinationFileBaseDir);
            foreach (var vault in vaults)
            {
                var srcVaultDir = Path.Combine(srcFileBaseDir, vault.Name);
                var dstVaultDir = Path.Combine(destinationFileBaseDir, vault.Name);
                if (Directory.Exists(srcVaultDir)) DirectoryCopy(srcVaultDir, dstVaultDir, true);
            }
        }

        public override string GetFormatId()
        {
            return "yml";
        }

        public override string GetFormatName()
        {
            return "YAML";
        }

        public override bool CanDeserializeFrom(string sourceDirectory)
        {
            return File.Exists(Path.Combine(sourceDirectory, "info.yml"));
        }

        protected override IEnumerable<string> GetDataFilePaths(string directory)
        {
            return Directory.GetFiles(directory, "*.yml");
        }

        protected override async Task<IEnumerable<SerializedCollection>> LoadDataFileAsync(string path,
            Database database, VltClass vltClass)
        {
            var deserializer = new DeserializerBuilder()
                .WithTypeInspector(inspector => new VltClassSchemaTypeInspector(inspector, database, vltClass))
                .Build();

            var results = deserializer.Deserialize<List<CustomSerializedCollection>>(
                await File.ReadAllTextAsync(path));

            return results.Select(ConvertFromCustomSerializedCollection);
        }

        private static SerializedCollection ConvertFromCustomSerializedCollection(CustomSerializedCollection data)
        {
            return new SerializedCollection
            {
                ParentName = data.ParentName,
                Name = data.Name,
                Data = data.Data.GetEntries()
            };
        }

        private static void ConvertVltCollectionsToSerializedCollections(string directory,
            IEnumerable<VltCollection> vltCollections, ICollection<CustomSerializedCollection> serializedCollections)
        {
            foreach (var vltCollection in vltCollections)
            {
                var serializedCollection = new CustomSerializedCollection()
                {
                    Name = vltCollection.Name,
                    ParentName = vltCollection.Parent?.Name,
                    Data = new CustomSerializedCollectionData()
                };

                foreach (var entry in vltCollection.GetOrderedData())
                {
                    serializedCollection.Data.SetEntry(entry.Key,
                        ConvertVltValueToSerializedValue(directory, vltCollection, vltCollection.Class[entry.Key],
                            entry.Value));
                }

                serializedCollections.Add(serializedCollection);
            }
        }

        private static object ConvertVltValueToSerializedValue(string directory, VltCollection collection,
            VltClassField field, object vltValue)
        {
            return vltValue switch
            {
                IStringValue stringValue => stringValue.GetString(),
                BaseBlob blob => ProcessBlob(directory, collection, field, blob),
                VltArrayType array => ConvertVltArrayToSerializedArray(directory, collection, field, array),
                _ => vltValue
            };
        }

        private static object ConvertVltArrayToSerializedArray(string directory, VltCollection collection,
            VltClassField field,
            VltArrayType array)
        {
            var listItemType = CloakingHelper.IsTypeAStringInDisguise(array.ItemType) ? typeof(string) : array.ItemType;
            var listType = typeof(List<>).MakeGenericType(listItemType);
            var items = (IList)Activator.CreateInstance(listType);

            if (items == null) throw new Exception("Activator.CreateInstance returned null");

            foreach (var arrayItem in array.Items)
            {
                items.Add(ConvertVltValueToSerializedValue(directory, collection, field, arrayItem));
            }

            return Activator.CreateInstance(typeof(CustomSerializedArray<>).MakeGenericType(listItemType),
                array.Capacity, items);
        }

        private static object ProcessBlob(string directory, VltCollection collection, VltClassField field,
            BaseBlob blob)
        {
            if (blob.Data is not { Length: > 0 })
            {
                return "";
            }

            var blobDir = Path.Combine(directory, "_blobs");
            Directory.CreateDirectory(blobDir);
            var blobPath = Path.Combine(blobDir,
                $"{collection.ShortPath.TrimEnd('/', '\\').Replace('/', '_').Replace('\\', '_')}_{field.Name}.bin");

            File.WriteAllBytes(blobPath, blob.Data);

            return blobPath[(directory.Length + 1)..];
        }

        protected override object ConvertSerializedValueToDataValue(Database database, string gameId, string dir,
            VltClass vltClass,
            VltClassField field,
            VltCollection vltCollection, object serializedValue, bool createInstance = true)
        {
            if (serializedValue == null)
                throw new ArgumentNullException(nameof(serializedValue), "serializedValue cannot be null");

            var resolvedType = database.TypeRegistry.ResolveType(field.TypeName);

            if (!CloakingHelper.IsTypeAStringInDisguise(resolvedType))
            {
                return !field.IsArray
                    ? serializedValue
                    : ConvertSerializedArrayToVltArray(field, serializedValue, resolvedType);
            }

            return CloakingHelper.UncloakObject(database, dir, field, serializedValue, resolvedType);
        }

        private static object ConvertSerializedArrayToVltArray(VltClassField field, object serializedValue,
            Type resolvedType)
        {
            var array = (ISerializedArray)serializedValue;

            foreach (var item in array.GetRawItems())
            {
                Debug.Assert(item.GetType() == resolvedType);
            }

            return new VltArrayType(field, resolvedType)
            {
                Items = array.GetRawItems().ToList(),
                Capacity = array.GetCapacity()
            };
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            var dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);

            var dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.       
            Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
                foreach (var subdir in dirs)
                {
                    var tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, true);
                }
        }
    }
}