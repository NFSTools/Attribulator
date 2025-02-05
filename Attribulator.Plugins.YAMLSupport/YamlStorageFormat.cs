using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Attribulator.API.Data;
using Attribulator.API.Serialization;
using Attribulator.API.Utils;
using Attribulator.Plugins.YAMLSupport.Helpers;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;
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
        private static SerializerBuilder CreateDefaultSerializerBuilder<TKey>() where TKey : struct, IKey<TKey>
        {
            return new SerializerBuilder()
                .WithTypeConverter(new VltKeyTypeConverter<Key32>())
                .WithTypeConverter(new VltKeyTypeConverter<Key64>())
                .WithTypeConverter(new VltKeyTypeConverter<BinKey32>())
                .WithTypeConverter(new VltKeyTypeConverter<BinKey64>())
                .WithAttributeOverride<Matrix4x4>(m => m.Translation, new YamlIgnoreAttribute())
                .WithAttributeOverride<Matrix4x4>(m => m.IsIdentity, new YamlIgnoreAttribute())
                .WithQuotingNecessaryStrings(true)
                .EnsureRoundtrip()
                .DisableAliases();
        }

        private static DeserializerBuilder CreateDefaultDeserializerBuilder<TKey>() where TKey : struct, IKey<TKey>
        {
            return new DeserializerBuilder()
                .WithEnforceRequiredMembers()
                .WithTypeConverter(new VltKeyTypeConverter<Key32>())
                .WithTypeConverter(new VltKeyTypeConverter<Key64>())
                .WithTypeConverter(new VltKeyTypeConverter<BinKey32>())
                .WithTypeConverter(new VltKeyTypeConverter<BinKey64>());
        }

        public override SerializedDatabaseInfo LoadInfo<TKey>(string sourceDirectory,
            Database<TKey> destinationDatabase)
        {
            using var dbs = new StreamReader(Path.Combine(sourceDirectory, "info.yml"));

            var deserializer = CreateDefaultDeserializerBuilder<TKey>().Build();
            var serializer = CreateDefaultSerializerBuilder<TKey>().Build();

            // Insane strategy to get proper static values:
            // 1. Read the schema with StaticValue as an object. Complex types turn into dictionaries.
            // 2. Once we know the type of every field (after reading the schema the first time), re-serialize all the
            //    static values and deserialize them AGAIN, this time with their proper types. (Except for the special cases.)
            // 3. Replace the original StaticValues with the new, (almost) properly typed ones.
            // 4. Profit.
            var serializedDatabaseInfo = deserializer.Deserialize<SerializedDatabaseInfo>(dbs);

            foreach (var serializedDatabaseClass in serializedDatabaseInfo.Classes)
            {
                var classKey = KeyUtils.StringToKey<TKey>(serializedDatabaseClass.Name);
                foreach (var serializedDatabaseClassField in serializedDatabaseClass.Fields)
                {
                    if ((serializedDatabaseClassField.Flags & DefinitionFlags.IsStatic) == 0)
                    {
                        continue;
                    }

                    var fieldKey = KeyUtils.StringToKey<TKey>(serializedDatabaseClassField.Name);
                    var fieldTypeKey = KeyUtils.StringToKey<TKey>(serializedDatabaseClassField.TypeName);
                    var fieldUnderlyingType =
                        destinationDatabase.TypeRegistry.ResolveFieldType(classKey, fieldKey, fieldTypeKey);

                    var effectiveFieldUnderlyingType = CloakingHelper.IsTypeAStringInDisguise<TKey>(fieldUnderlyingType)
                        ? typeof(string)
                        : fieldUnderlyingType;

                    var staticType = (serializedDatabaseClassField.Flags & DefinitionFlags.Array) != 0
                        ? typeof(CustomSerializedArray<>).MakeGenericType(fieldUnderlyingType)
                        : effectiveFieldUnderlyingType;

                    var serializedStaticValue = serializer.Serialize(serializedDatabaseClassField.StaticValue);

                    try
                    {
                        serializedDatabaseClassField.StaticValue =
                            deserializer.Deserialize(serializedStaticValue, staticType);
                    }
                    catch (Exception e)
                    {
                        throw new SerializationException(
                            $"Error while deserializing static data for field {serializedDatabaseClassField.Name} in class {serializedDatabaseClass.Name}",
                            e);
                    }
                }
            }

            return serializedDatabaseInfo;
        }

        public override void Serialize<TKey>(Database<TKey> sourceDatabase, string destinationDirectory,
            IEnumerable<LoadedFile<TKey>> loadedFiles, Func<Vault<TKey>, bool> filterFunc = null)
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
            {
                Name = f.Name,
                Group = f.Group,
                Vaults = f.Vaults.Select(v => new SerializedVaultInfo
                {
                    Name = v.Name,
                    Version = v.Version
                }).ToList()
            }));

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
                    Name = KeyUtils.KeyToString(databaseClass.Key),
                    LayoutSize = databaseClass.LayoutSize,
                    StaticSize = databaseClass.StaticSize,
                    Fields = new List<SerializedDatabaseClassField>(),
                };

                serializedDatabaseClass.Fields.AddRange(databaseClass.Fields.Values.Select(field =>
                    new SerializedDatabaseClassField
                    {
                        Name = KeyUtils.KeyToString(field.Key),
                        TypeName = KeyUtils.KeyToString(field.TypeKey),
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

            var infoSerializer = CreateDefaultSerializerBuilder<TKey>().Build();

            using var sw = new StreamWriter(Path.Combine(destinationDirectory, "info.yml"));
            infoSerializer.Serialize(sw, serializedDatabaseInfo);

            var classSpecificSerializers = sourceDatabase.Classes.ToDictionary(c => c.Key, c =>
            {
                return CreateDefaultSerializerBuilder<TKey>()
                    .WithTypeInspector(
                        inspector => new VltClassSchemaTypeInspector<TKey>(inspector, sourceDatabase, c))
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
                                 .GroupBy(v => v.Class.Key))
                    {
                        var serializedCollections = new List<CustomSerializedCollection<TKey>>();
                        ConvertVltCollectionsToSerializedCollections(vaultDirectory, collectionGroup,
                            serializedCollections);

                        using var vw = new StreamWriter(Path.Combine(vaultDirectory,
                            KeyUtils.KeyToString(collectionGroup.Key) + ".yml"));
                        classSpecificSerializers[collectionGroup.Key].Serialize(vw, serializedCollections);
                    }
                }
            }
        }

        public override void Backup<TKey>(string srcDirectory, string destinationDirectory,
            LoadedFile<TKey> file,
            IEnumerable<Vault<TKey>> vaults)
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

        protected override async Task<IEnumerable<SerializedCollection<TKey>>> LoadDataFileAsync<TKey>(string path,
            Database<TKey> database, VltClass<TKey> vltClass)
        {
            var deserializer = CreateDefaultDeserializerBuilder<TKey>()
                .WithTypeInspector(inspector => new VltClassSchemaTypeInspector<TKey>(inspector, database, vltClass))
                .Build();

            var results = deserializer.Deserialize<List<CustomSerializedCollection<TKey>>>(
                await File.ReadAllTextAsync(path));

            return results.Select(ConvertFromCustomSerializedCollection);
        }

        private static SerializedCollection<TKey> ConvertFromCustomSerializedCollection<TKey>(
            CustomSerializedCollection<TKey> data) where TKey : struct, IKey<TKey>
        {
            return new SerializedCollection<TKey>
            {
                ParentName = data.ParentName,
                Name = data.Name,
                Data = data.Data.GetTable()
            };
        }

        private static string GetCollectionName<TKey>(VltCollection<TKey> collection) where TKey : struct, IKey<TKey>
        {
            if (!collection.HasEntry("CollectionName")
                || collection.GetRawValue<object>("CollectionName") is not string collectionName)
                return KeyUtils.KeyToString(collection.Key);
            return TKey.FromString(collectionName) == collection.Key
                ? collectionName
                : KeyUtils.KeyToString(collection.Key);
        }

        private static void ConvertVltCollectionsToSerializedCollections<TKey>(string directory,
            IEnumerable<VltCollection<TKey>> vltCollections,
            ICollection<CustomSerializedCollection<TKey>> serializedCollections) where TKey : struct, IKey<TKey>
        {
            foreach (var vltCollection in vltCollections)
            {
                var serializedCollection = new CustomSerializedCollection<TKey>
                {
                    Name = GetCollectionName(vltCollection),
                    ParentName = vltCollection.Parent is { } parent ? GetCollectionName(parent) : null,
                    Data = new CustomSerializedCollectionData<TKey>()
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

        private static object ConvertVltValueToSerializedValue<TKey>(string directory, VltCollection<TKey> collection,
            VltClassField<TKey> field, object vltValue) where TKey : struct, IKey<TKey>
        {
            return vltValue switch
            {
                IStringValue stringValue => stringValue.GetString(),
                BaseBlob<TKey> blob => ProcessBlob(directory, collection, field, blob),
                VltArrayType<TKey> array => ConvertVltArrayToSerializedArray(directory, collection, field, array),
                _ => vltValue
            };
        }

        private static object ConvertVltArrayToSerializedArray<TKey>(string directory, VltCollection<TKey> collection,
            VltClassField<TKey> field,
            VltArrayType<TKey> array) where TKey : struct, IKey<TKey>
        {
            var listItemType = CloakingHelper.IsTypeAStringInDisguise<TKey>(array.ItemType)
                ? typeof(string)
                : array.ItemType;
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

        private static object ProcessBlob<TKey>(string directory, VltCollection<TKey> collection,
            VltClassField<TKey> field,
            BaseBlob<TKey> blob) where TKey : struct, IKey<TKey>
        {
            if (blob.Data is not { Length: > 0 })
            {
                return "";
            }

            var className = KeyUtils.KeyToString(collection.Class.Key);
            var collectionName = KeyUtils.KeyToString(collection.Key);
            var fieldName = KeyUtils.KeyToString(field.Key);
            var collectionShortPath = $"{className}/{collectionName}";

            var blobDir = Path.Combine(directory, "_blobs");
            Directory.CreateDirectory(blobDir);
            var blobPath = Path.Combine(blobDir,
                $"{collectionShortPath.TrimEnd('/', '\\').Replace('/', '_').Replace('\\', '_')}_{fieldName}.bin");

            File.WriteAllBytes(blobPath, blob.Data);

            return blobPath[(directory.Length + 1)..];
        }

        protected override object ConvertSerializedValueToDataValue<TKey>(Database<TKey> database, string gameId,
            string dir,
            VltClass<TKey> vltClass,
            VltClassField<TKey> field,
            VltCollection<TKey> vltCollection, object serializedValue, bool createInstance = true)
        {
            if (serializedValue == null)
                throw new ArgumentNullException(nameof(serializedValue), "serializedValue cannot be null");

            var resolvedType = database.TypeRegistry.ResolveFieldType(field);

            if (!CloakingHelper.IsTypeAStringInDisguise<TKey>(resolvedType))
            {
                return !field.IsArray
                    ? serializedValue
                    : ConvertSerializedArrayToVltArray(field, serializedValue, resolvedType);
            }

            return CloakingHelper.UncloakObject(database, dir, field, serializedValue, resolvedType);
        }

        private static object ConvertSerializedArrayToVltArray<TKey>(VltClassField<TKey> field, object serializedValue,
            Type resolvedType) where TKey : struct, IKey<TKey>
        {
            var array = (ISerializedArray)serializedValue;

            foreach (var item in array.GetRawItems())
            {
                Debug.Assert(item.GetType() == resolvedType);
            }

            return new VltArrayType<TKey>(field, resolvedType)
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