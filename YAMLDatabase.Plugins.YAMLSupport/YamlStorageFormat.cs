using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using CoreLibraries.GameUtilities;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.DB;
using VaultLib.Core.Types;
using VaultLib.Core.Types.Attrib;
using VaultLib.Core.Types.EA.Reflection;
using VaultLib.Core.Utils;
using YAMLDatabase.API.Data;
using YAMLDatabase.API.Serialization;
using YAMLDatabase.API.Utils;
using YamlDotNet.Serialization;

namespace YAMLDatabase.Plugins.YAMLSupport
{
    /// <summary>
    ///     Implements the YAML storage format.
    /// </summary>
    /// TODO: This is in DESPERATE need of refactoring. Storage code needs to be as unified as possible.
    public class YamlStorageFormat : IDatabaseStorageFormat
    {
        public IEnumerable<LoadedFile> Deserialize(string sourceDirectory, Database destinationDatabase)
        {
            var deserializer = new DeserializerBuilder().Build();

            using var dbs = new StreamReader(Path.Combine(sourceDirectory, "info.yml"));
            var loadedDatabase = deserializer.Deserialize<SerializedDatabaseInfo>(dbs);
            var isX86 = destinationDatabase.Options.Type == DatabaseType.X86Database;

            foreach (var loadedDatabaseClass in loadedDatabase.Classes)
            {
                var vltClass = new VltClass(loadedDatabaseClass.Name);

                foreach (var loadedDatabaseClassField in loadedDatabaseClass.Fields)
                {
                    var field = new VltClassField(
                        isX86
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
                }

                destinationDatabase.AddClass(vltClass);
            }

            foreach (var loadedDatabaseType in loadedDatabase.Types)
                destinationDatabase.Types.Add(new DatabaseTypeInfo
                    {Name = loadedDatabaseType.Name, Size = loadedDatabaseType.Size});

            var collectionParentDictionary = new Dictionary<string, string>();
            var collectionDictionary = new Dictionary<string, VltCollection>();
            var vaultsToSaveDictionary = new Dictionary<string, List<Vault>>();
            var collectionsToBeAdded = new List<VltCollection>();

            foreach (var file in loadedDatabase.Files)
            {
                var baseDirectory = Path.Combine(sourceDirectory, file.Group, file.Name);
                vaultsToSaveDictionary[file.Name] = new List<Vault>();
                foreach (var vault in file.Vaults)
                {
                    var vaultDirectory = Path.Combine(baseDirectory, vault).Trim();
                    var newVault = new Vault(vault) {Database = destinationDatabase, IsPrimaryVault = vault == "db"};
                    if (Directory.Exists(vaultDirectory))
                    {
                        var trackedCollections = new HashSet<string>();

                        foreach (var dataFile in Directory.GetFiles(vaultDirectory, "*.yml"))
                        {
                            var className = Path.GetFileNameWithoutExtension(dataFile);
                            var vltClass = destinationDatabase.FindClass(className);

                            if (vltClass == null)
                                throw new InvalidDataException($"Unknown class: {className} ({dataFile})");

                            //#if DEBUG
                            //                        Debug.WriteLine("Processing class '{0}' in vault '{1}' (file: {2})", className, vault, dataFile);
                            //#else
                            //                        Console.WriteLine("Processing class '{0}' in vault '{1}' (file: {2})", className, vault, dataFile);
                            //#endif

                            using var vr = new StreamReader(dataFile);
                            var collections = deserializer.Deserialize<List<SerializedCollection>>(vr);

                            foreach (var loadedCollection in collections)
                            {
                                // BUG 16.02.2020: we have to do this to get around a YamlDotNet bug
                                if (loadedCollection.Name == null)
                                    loadedCollection.Name = "null";

                                foreach (var k in loadedCollection.Data.Keys.ToList()
                                    .Where(k => loadedCollection.Data[k] == null))
                                    loadedCollection.Data[k] = "null";
                            }

                            var newCollections = new List<VltCollection>();

                            void AddCollectionsToList(ICollection<VltCollection> collectionList,
                                IEnumerable<SerializedCollection> collectionsToAdd)
                            {
                                if (collectionList == null)
                                    throw new Exception("collectionList should not be null!");
                                collectionsToAdd ??= new List<SerializedCollection>();

                                foreach (var loadedCollection in collectionsToAdd)
                                {
                                    var newVltCollection = new VltCollection(newVault, vltClass, loadedCollection.Name);

                                    foreach (var (key, value) in loadedCollection.Data)
                                    {
                                        if (!vltClass.TryGetField(key, out var field))
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

                            AddCollectionsToList(newCollections, collections);


                            foreach (var newCollection in newCollections)
                            {
                                if (!trackedCollections.Add(newCollection.ShortPath))
                                    throw new Exception(
                                        $"Duplicate collection found! Multiple collections at '{newCollection.ShortPath}' have been defined in your YML files.");

                                collectionsToBeAdded.Add(newCollection);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("WARN: vault {0} has no folder; looked for {1}", vault, vaultDirectory);
                    }

                    vaultsToSaveDictionary[file.Name].Add(newVault);
                    destinationDatabase.Vaults.Add(newVault);
                }

                yield return new LoadedFile(file.Name, file.Group, vaultsToSaveDictionary[file.Name]);
            }

            // dependency resolution
            var resolved = new List<VaultDependencyNode>();
            var unresolved = new List<VaultDependencyNode>();

            foreach (var vault in destinationDatabase.Vaults)
            {
                var vaultCollections = collectionsToBeAdded.Where(c => c.Vault.Name == vault.Name).ToList();
                var node = new VaultDependencyNode(vault);

                foreach (var vaultCollection in vaultCollections)
                {
                    var parentKey = collectionParentDictionary[vaultCollection.ShortPath];

                    if (!string.IsNullOrEmpty(parentKey))
                    {
                        var parentCollection = collectionDictionary[$"{vaultCollection.Class.Name}/{parentKey}"];
                        if (parentCollection.Vault.Name != vault.Name)
                            node.AddEdge(new VaultDependencyNode(parentCollection.Vault));
                    }
                }

                ResolveDependencies(node, resolved, unresolved);

                Debug.WriteLine("Vault {0}: {1} collections", vault.Name, vaultCollections.Count);
            }

            resolved = resolved.Distinct(VaultDependencyNode.VaultComparer).ToList();
            unresolved = unresolved.Distinct(VaultDependencyNode.VaultComparer).ToList();

            if (unresolved.Count != 0) throw new Exception("Cannot continue loading - unresolved vault dependencies");

            foreach (var node in resolved)
            {
                var vault = node.Vault;
                var vaultCollections = collectionsToBeAdded.Where(c => c.Vault.Name == vault.Name).ToList();

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

        private void ResolveDependencies(VaultDependencyNode node, List<VaultDependencyNode> resolved,
            List<VaultDependencyNode> unresolved)
        {
            unresolved.Add(node);

            foreach (var edge in node.Edges)
                if (!resolved.Contains(edge))
                    ResolveDependencies(edge, resolved, unresolved);

            resolved.Add(node);
            unresolved.Remove(node);
        }

        private VLTBaseType ConvertSerializedValueToDataValue(Database database, string gameId, string dir,
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
                        : DoDictionaryConversion(gameId, dir, vltClass, field, vltCollection, instance, dictionary));
            }

            throw new InvalidDataException("Could not convert serialized value of type: " + serializedValue.GetType());
        }

        private VLTArrayType DoArrayConversion(Database database, string gameId, string dir, VltClass vltClass,
            VltClassField field,
            VltCollection vltCollection, VLTArrayType array, Dictionary<object, object> dictionary)
        {
            var capacity = ushort.Parse(dictionary["Capacity"].ToString());
            var rawItemList = (List<object>) dictionary["Data"];

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

        private object DoDictionaryConversion(string gameId, string dir, VltClass vltClass, VltClassField field,
            VltCollection vltCollection, object instance, Dictionary<object, object> dictionary)
        {
            foreach (var pair in dictionary)
            {
                var propName = (string) pair.Key;
                var propertyInfo =
                    instance.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);

                if (propertyInfo == null)
                    throw new InvalidDataException(
                        $"Cannot set unknown property of '{instance.GetType()}': '{propName}'");

                if (propertyInfo.SetMethod == null || !propertyInfo.SetMethod.IsPublic) continue;

                var propType = propertyInfo.PropertyType;

                if (propType.IsEnum)
                {
                    propertyInfo.SetValue(instance, Enum.Parse(propType, pair.Value.ToString()));
                }
                else if (propType.IsPrimitive || propType == typeof(string))
                {
                    var newValue = FixUpValueForComplexObject(pair.Value, propType);
                    propertyInfo.SetValue(instance,
                        Convert.ChangeType(newValue, propType, CultureInfo.InvariantCulture));
                }
                else if (pair.Value is List<object> objects)
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

                            //newList[index] = FixUpValueForComplexObject(objects[index]);
                            //newList[index] = Convert.ChangeType(objects[index].ToString(), elemType, CultureInfo.InvariantCulture);
                        }

                    propertyInfo.SetValue(instance, newList);
                }
                else if (pair.Value is Dictionary<object, object> objectDictionary)
                {
                    var propInstance = propType.IsSubclassOf(typeof(VLTBaseType))
                        ? TypeRegistry.ConstructInstance(propType, vltClass, field, vltCollection)
                        : Activator.CreateInstance(propType);

                    propertyInfo.SetValue(instance,
                        DoDictionaryConversion(gameId, dir, vltClass, field, vltCollection, propInstance,
                            objectDictionary));
                }
                else if (pair.Value != null)
                {
                    throw new Exception();
                }
            }

            return instance;
        }

        private static object FixUpValueForComplexObject(object value, Type elemType)
        {
            if (value is string s)
                if (s.StartsWith("0x") && elemType == typeof(uint))
                    return uint.Parse(s.Substring(2), NumberStyles.AllowHexSpecifier);

            return value;
        }

        public class SerializedArrayWrapper
        {
            public ushort Capacity { get; set; }
            public IList Data { get; set; }
        }

        public class VaultDependencyNode
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