using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Attribulator.API.Data;
using Attribulator.API.Serialization;
using Attribulator.API.Utils;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.DB;
using VaultLib.Core.Types;
using VaultLib.Core.Types.Attrib;
using VaultLib.Core.Types.EA.Reflection;
using VaultLib.Core.Utils;
using YamlDotNet.Serialization;

namespace Attribulator.Plugins.YAMLSupport
{
    /// <summary>
    ///     Implements the YAML storage format.
    /// </summary>
    public class YamlStorageFormat : BaseStorageFormat
    {
        private static readonly IDeserializer Deserializer = new DeserializerBuilder().Build();

        public override SerializedDatabaseInfo LoadInfo(string sourceDirectory)
        {
            var deserializer = new DeserializerBuilder().Build();

            using var dbs = new StreamReader(Path.Combine(sourceDirectory, "info.yml"));
            return deserializer.Deserialize<SerializedDatabaseInfo>(dbs);
        }

        public override void Serialize(Database sourceDatabase, string destinationDirectory,
            IEnumerable<LoadedFile> loadedFiles)
        {
            var loadedFileList = loadedFiles.ToList();
            var loadedDatabase = new SerializedDatabaseInfo
            {
                Classes = new List<SerializedDatabaseClass>(),
                Files = new List<SerializedDatabaseFile>(),
                Types = new List<SerializedTypeInfo>(),
                PrimaryVaultName = loadedFileList.SelectMany(f => f.Vaults).First(v => v.IsPrimaryVault).Name
            };

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

        protected override async Task<IEnumerable<SerializedCollection>> LoadDataFileAsync(string path)
        {
            var collections =
                Deserializer.Deserialize<List<SerializedCollection>>(
                    await File.ReadAllTextAsync(path));

            // Fix false null values
            foreach (var loadedCollection in collections)
            {
                loadedCollection.Name ??= "null";

                foreach (var k in loadedCollection.Data.Keys.ToList()
                    .Where(k => loadedCollection.Data[k] == null))
                    loadedCollection.Data[k] = "null";
            }

            return collections;
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

                    if (instance == null) throw new Exception("Activator.CreateInstance returned null");

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

        protected override VLTBaseType ConvertSerializedValueToDataValue(Database database, string gameId, string dir,
            VltClass vltClass,
            VltClassField field,
            VltCollection vltCollection, object serializedValue, bool createInstance = true)
        {
            //    0. Is it null? Bail out right away.
            //    1. Is it a string? Determine underlying primitive type, and then convert.
            //    2. Is it a list? Ensure we have an array, and then convert all values RECURSIVELY.
            //    3. Is it a dictionary? Convert and set all values RECURSIVELY, ignoring ones that cannot be set at runtime.
            //    4. Are none of those conditions true? Bail out.

            if (serializedValue == null) throw new InvalidDataException("Null serializedValue is NOT PERMITTED!");

            // Create a new data instance
            var instance = createInstance
                ? TypeRegistry.CreateInstance(database.Options.GameId, vltClass, field, vltCollection)
                : TypeRegistry.ConstructInstance(TypeRegistry.ResolveType(gameId, field.TypeName), vltClass, field,
                    vltCollection);

            return DoValueConversion(database, gameId, dir, vltClass, field, vltCollection, serializedValue, instance);
        }

        private VLTBaseType DoValueConversion(Database database, string gameId, string dir, VltClass vltClass,
            VltClassField field,
            VltCollection vltCollection,
            object serializedValue, object instance)
        {
            switch (serializedValue)
            {
                case string str:
                    switch (instance)
                    {
                        case IStringValue stringValue:
                            stringValue.SetString(str);
                            return (VLTBaseType) instance;
                        case PrimitiveTypeBase primitiveTypeBase:
                            return ValueConversionUtils.DoPrimitiveConversion(primitiveTypeBase, str);
                        case BaseBlob blob:
                        {
                            if (string.IsNullOrWhiteSpace(str)) return blob;

                            str = Path.Combine(dir, str);
                            if (!File.Exists(str))
                                throw new InvalidDataException(
                                    $"Could not locate blob data file for {vltCollection.ShortPath}[{field.Name}]");

                            blob.Data = File.ReadAllBytes(str);

                            return blob;
                        }
                    }

                    break;
                case Dictionary<object, object> dictionary:
                    return (VLTBaseType) (instance is VLTArrayType array
                        ? DoArrayConversion(database, gameId, dir, vltClass, field, vltCollection, array, dictionary)
                        : DoDictionaryConversion(vltClass, field, vltCollection, instance, dictionary));
            }

            throw new InvalidDataException("Could not convert serialized value of type: " + serializedValue.GetType());
        }

        private VLTArrayType DoArrayConversion(Database database, string gameId, string dir, VltClass vltClass,
            VltClassField field,
            VltCollection vltCollection, VLTArrayType array, Dictionary<object, object> dictionary)
        {
            var capacity = ushort.Parse(dictionary["Capacity"].ToString()!);
            var rawItemList = (List<object>) dictionary["Data"];

            if (capacity < rawItemList.Count)
                throw new InvalidDataException(
                    $"In collection {vltCollection.ShortPath}, the capacity of array field [{field.Name}] ({capacity}) is less than the number of elements in the array ({rawItemList.Count}).");
            if (field.MaxCount > 0 && (capacity > field.MaxCount || rawItemList.Count > field.MaxCount))
                throw new InvalidDataException(
                    $"In collection {vltCollection.ShortPath}, the size or capacity of array field [{field.Name}] is greater than the allowed size ({field.MaxCount}).");
            array.Capacity = capacity;
            array.Items = new List<VLTBaseType>();
            array.ItemAlignment = field.Alignment;
            array.FieldSize = field.Size;

            foreach (var o in rawItemList)
            {
                var newArrayItem =
                    ConvertSerializedValueToDataValue(database, gameId, dir, vltClass, field, vltCollection, o, false);

                array.Items.Add(newArrayItem);
            }

            return array;
        }

        private static object DoDictionaryConversion(VltClass vltClass, VltClassField field,
            VltCollection vltCollection, object instance, Dictionary<object, object> dictionary)
        {
            foreach (var (key, value) in dictionary)
            {
                var propName = (string) key;
                var propertyInfo =
                    instance.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);

                if (propertyInfo == null)
                    throw new InvalidDataException(
                        $"Cannot set unknown property of '{instance.GetType()}': '{propName}'");

                if (propertyInfo.SetMethod == null || !propertyInfo.SetMethod.IsPublic) continue;

                var propType = propertyInfo.PropertyType;

                if (propType.IsEnum)
                {
                    propertyInfo.SetValue(instance, Enum.Parse(propType, value.ToString()));
                }
                else if (propType.IsPrimitive || propType == typeof(string))
                {
                    var newValue = FixUpValueForComplexObject(value, propType);
                    propertyInfo.SetValue(instance,
                        Convert.ChangeType(newValue, propType, CultureInfo.InvariantCulture));
                }
                else
                {
                    switch (value)
                    {
                        case List<object> objects:
                        {
                            var newList = (IList) Activator.CreateInstance(propType, objects.Count);
                            var elemType = propType.GetElementType() ?? throw new Exception();

                            for (var index = 0; index < objects.Count; index++)
                                if (elemType.IsEnum)
                                {
                                    newList[index] = Enum.Parse(elemType, objects[index].ToString());
                                }
                                else
                                {
                                    if (elemType == typeof(string))
                                    {
                                        newList[index] = objects[index];
                                    }
                                    else
                                    {
                                        var fixedValue = FixUpValueForComplexObject(objects[index], elemType);
                                        var convertedValue =
                                            Convert.ChangeType(fixedValue, elemType, CultureInfo.InvariantCulture);

                                        newList[index] = convertedValue;
                                    }
                                }

                            propertyInfo.SetValue(instance, newList);
                            break;
                        }
                        case Dictionary<object, object> objectDictionary:
                        {
                            var propInstance = propType.IsSubclassOf(typeof(VLTBaseType))
                                ? TypeRegistry.ConstructInstance(propType, vltClass, field, vltCollection)
                                : Activator.CreateInstance(propType);

                            propertyInfo.SetValue(instance,
                                DoDictionaryConversion(vltClass, field, vltCollection, propInstance,
                                    objectDictionary));
                            break;
                        }
                        default:
                        {
                            if (value != null) throw new Exception();

                            break;
                        }
                    }
                }
            }

            return instance;
        }

        private static object FixUpValueForComplexObject(object value, Type elemType)
        {
            if (value is string s)
                if (s.StartsWith("0x", StringComparison.Ordinal) && elemType == typeof(uint))
                    return uint.Parse(s.Substring(2), NumberStyles.AllowHexSpecifier);

            return value;
        }

        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
        public class SerializedArrayWrapper
        {
            public ushort Capacity { get; set; }
            public IList Data { get; set; }
        }
    }
}