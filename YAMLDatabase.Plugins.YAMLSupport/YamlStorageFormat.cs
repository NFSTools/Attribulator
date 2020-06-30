using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using VaultLib.Core.Data;
using VaultLib.Core.DB;
using VaultLib.Core.Types;
using VaultLib.Core.Types.Attrib;
using VaultLib.Core.Types.EA.Reflection;
using VaultLib.Core.Utils;
using YAMLDatabase.API.Data;
using YAMLDatabase.API.Serialization;
using YamlDotNet.Serialization;

namespace YAMLDatabase.Plugins.YAMLSupport
{
    /// <summary>
    ///     Implements the YAML storage format.
    /// </summary>
    public class YamlStorageFormat : IDatabaseStorageFormat
    {
        public SerializedDatabaseInfo Deserialize(string sourceDirectory, Database destinationDatabase)
        {
            throw new NotImplementedException();
        }

        public void Serialize(Database sourceDatabase, string destinationDirectory, IEnumerable<LoadedFile> loadedFiles)
        {
            var loadedDatabase = new SerializedDatabaseInfo
            {
                Classes = new List<SerializedDatabaseClass>(),
                Files = new List<SerializedDatabaseFile>(),
                Types = new List<SerializedTypeInfo>()
            };

            var loadedFileList = loadedFiles.ToList();

            loadedDatabase.Files.AddRange(loadedFileList.Select(f => new SerializedDatabaseFile
                {Name = f.Name, Group = f.Group, Vaults = f.Vaults.Select(v => v.Name).ToList()}));

            foreach (var databaseType in sourceDatabase.Types)
                loadedDatabase.Types.Add(new SerializedTypeInfo
                {
                    Name = databaseType.Name,
                    Size = databaseType.Size
                });

            foreach (var databaseClass in sourceDatabase.Classes)
            {
                var loadedDatabaseClass = new SerializedDatabaseClass
                {
                    Name = databaseClass.Name,
                    Fields = new List<SerializedDatabaseClassField>()
                };

                loadedDatabaseClass.Fields.AddRange(databaseClass.Fields.Values.Select(field =>
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
                            ConvertDataValueToSerializedValue(destinationDirectory, null, field, field.StaticValue)
                    }));

                loadedDatabase.Classes.Add(loadedDatabaseClass);
            }

            var serializerBuilder = new SerializerBuilder();
            var serializer = serializerBuilder.Build();

            using var sw = new StreamWriter(Path.Combine(destinationDirectory, "info.yml"));
            serializer.Serialize(sw, loadedDatabase);

            foreach (var loadedDatabaseFile in loadedFileList)
            {
                var baseDirectory =
                    Path.Combine(destinationDirectory, loadedDatabaseFile.Group, loadedDatabaseFile.Name);
                Directory.CreateDirectory(baseDirectory);

                foreach (var vault in loadedDatabaseFile.Vaults)
                {
                    var vaultDirectory = Path.Combine(baseDirectory, vault.Name).Trim();
                    Directory.CreateDirectory(vaultDirectory);

                    // Problem: Gameplay data is separated into numerous vaults, so we can't easily construct a proper hierarchy
                    // Solution: Store the name of the parent node instead of having an array of children.

                    foreach (var collectionGroup in sourceDatabase.RowManager.GetCollectionsInVault(vault)
                        .GroupBy(v => v.Class.Name))
                    {
                        var loadedCollections = new List<SerializedCollection>();
                        AddLoadedCollections(vaultDirectory, loadedCollections, collectionGroup);

                        using var vw = new StreamWriter(Path.Combine(vaultDirectory, collectionGroup.Key + ".yml"));
                        serializer.Serialize(vw, loadedCollections);
                    }
                }
            }
        }

        public string GetFormatId()
        {
            return "yml";
        }

        public string GetFormatName()
        {
            return "YAML";
        }

        public bool CanDeserializeFrom(string sourceDirectory)
        {
            return File.Exists(Path.Combine(sourceDirectory, "info.yml"));
        }

        private void AddLoadedCollections(string directory, ICollection<SerializedCollection> loadedVaultCollections,
            IEnumerable<VltCollection> vltCollections)
        {
            foreach (var vltCollection in vltCollections)
            {
                var loadedCollection = new SerializedCollection
                {
                    Name = vltCollection.Name,
                    ParentName = vltCollection.Parent?.Name,
                    Data = new Dictionary<string, object>()
                };

                foreach (var (key, value) in vltCollection.GetData())
                    loadedCollection.Data[key] =
                        ConvertDataValueToSerializedValue(directory, vltCollection, vltCollection.Class[key], value);

                loadedVaultCollections.Add(loadedCollection);
            }
        }

        private object ConvertDataValueToSerializedValue(string directory, VltCollection collection,
            VltClassField field, VLTBaseType dataPairValue)
        {
            switch (dataPairValue)
            {
                case IStringValue stringValue:
                    return stringValue.GetString();
                case PrimitiveTypeBase ptb:
                    return ptb.GetValue();
                case BaseBlob blob:
                    return ProcessBlob(directory, collection, field, blob);
                case VLTArrayType array:
                {
                    var listType = typeof(List<>);
                    var listGenericType = ResolveType(array.ItemType);
                    var constructedListType = listType.MakeGenericType(listGenericType);
                    var instance = (IList) Activator.CreateInstance(constructedListType);

                    foreach (var arrayItem in array.Items)
                        instance.Add(listGenericType.IsPrimitive || listGenericType.IsEnum ||
                                     listGenericType == typeof(string)
                            ? ConvertDataValueToSerializedValue(directory, collection, field, arrayItem)
                            : arrayItem);

                    return new SerializedArrayWrapper
                    {
                        Capacity = array.Capacity,
                        Data = instance
                    };
                }
                default:
                    return dataPairValue;
            }
        }

        private object ProcessBlob(string directory, VltCollection collection, VltClassField field, BaseBlob blob)
        {
            if (blob.Data != null && blob.Data.Length > 0)
            {
                var blobDir = Path.Combine(directory, "_blobs");
                Directory.CreateDirectory(blobDir);
                var blobPath = Path.Combine(blobDir,
                    $"{collection.ShortPath.TrimEnd('/', '\\').Replace('/', '_').Replace('\\', '_')}_{field.Name}.bin");

                File.WriteAllBytes(blobPath, blob.Data);

                return blobPath.Substring(directory.Length + 1);
            }

            return "";
        }

        private static Type ResolveType(Type type)
        {
            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(VLTEnumType<>)) return type.GetGenericArguments()[0];
            }
            else if (type.BaseType == typeof(PrimitiveTypeBase))
            {
                var info = type.GetCustomAttributes<PrimitiveInfoAttribute>().First();

                return info.PrimitiveType;
            }

            return type;
        }

        public class SerializedArrayWrapper
        {
            public ushort Capacity { get; set; }
            public IList Data { get; set; }
        }
    }
}