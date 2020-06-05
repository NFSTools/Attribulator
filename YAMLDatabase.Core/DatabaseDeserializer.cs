using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.DB;
using VaultLib.Core.Hashing;
using VaultLib.Core.Types;
using VaultLib.Core.Types.Attrib;
using VaultLib.Core.Types.EA.Reflection;
using VaultLib.Core.Utils;
using YAMLDatabase.Core.Algorithm;
using YAMLDatabase.Core.Utils;
using YamlDotNet.Serialization;

namespace YAMLDatabase.Core
{
    /// <summary>
    /// Deserializes YAML files to a <see cref="VaultLib.Core.DB.Database"/> 
    /// </summary>
    public class DatabaseDeserializer
    {
        private readonly Database _database;
        private readonly string _inputDirectory;

        private LoadedDatabase _loadedDatabase;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseDeserializer"/> class.
        /// </summary>
        /// <param name="database">The database to serialize.</param>
        /// <param name="directory">The directory to read YAML files from.</param>
        public DatabaseDeserializer(Database database, string directory)
        {
            _database = database;
            _inputDirectory = directory;
        }

        /// <summary>
        /// Deserializes the files.
        /// </summary>
        public LoadedDatabase Deserialize()
        {
            var deserializer = new DeserializerBuilder().Build();

            using var dbs = new StreamReader(Path.Combine(_inputDirectory, "info.yml"));
            var loadedDatabase = deserializer.Deserialize<LoadedDatabase>(dbs);
            var isX86 = _database.Options.Type == DatabaseType.X86Database;

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
                    {
                        field.StaticValue = ConvertSerializedValueToDataValue(_database.Options.GameId, _inputDirectory,
                            vltClass, field, null,
                            loadedDatabaseClassField.StaticValue);
                    }

                    vltClass.Fields.Add(field.Key, field);
                }

                _database.AddClass(vltClass);
            }

            foreach (var loadedDatabaseType in loadedDatabase.Types)
            {
                _database.Types.Add(new DatabaseTypeInfo
                    {Name = loadedDatabaseType.Name, Size = loadedDatabaseType.Size});
            }

            var collectionParentDictionary = new Dictionary<string, string>();
            var collectionDictionary = new Dictionary<string, VltCollection>();
            var vaultsToSaveDictionary = new Dictionary<string, List<Vault>>();
            var collectionsToBeAdded = new List<VltCollection>();

            foreach (var file in loadedDatabase.Files)
            {
                file.LoadedVaults = new List<Vault>();

                var baseDirectory = Path.Combine(_inputDirectory, file.Group, file.Name);
                vaultsToSaveDictionary[file.Name] = new List<Vault>();
                foreach (var vault in file.Vaults)
                {
                    var vaultDirectory = Path.Combine(baseDirectory, vault).Trim();
                    var newVault = new Vault(vault) {Database = _database, IsPrimaryVault = vault == "db"};
                    if (Directory.Exists(vaultDirectory))
                    {
                        HashSet<string> trackedCollections = new HashSet<string>();

                        foreach (var dataFile in Directory.GetFiles(vaultDirectory, "*.yml"))
                        {
                            var className = Path.GetFileNameWithoutExtension(dataFile);
                            var vltClass = _database.FindClass(className);

                            if (vltClass == null)
                            {
                                throw new InvalidDataException($"Unknown class: {className} ({dataFile})");
                            }

                            //#if DEBUG
                            //                        Debug.WriteLine("Processing class '{0}' in vault '{1}' (file: {2})", className, vault, dataFile);
                            //#else
                            //                        Console.WriteLine("Processing class '{0}' in vault '{1}' (file: {2})", className, vault, dataFile);
                            //#endif

                            using var vr = new StreamReader(dataFile);
                            var collections = deserializer.Deserialize<List<LoadedCollection>>(vr);

                            foreach (var loadedCollection in collections)
                            {
                                // BUG 16.02.2020: we have to do this to get around a YamlDotNet bug
                                if (loadedCollection.Name == null)
                                    loadedCollection.Name = "null";

                                foreach (var k in loadedCollection.Data.Keys.ToList()
                                    .Where(k => loadedCollection.Data[k] == null))
                                {
                                    loadedCollection.Data[k] = "null";
                                }
                            }

                            var newCollections = new List<VltCollection>();

                            void AddCollectionsToList(ICollection<VltCollection> collectionList,
                                IEnumerable<LoadedCollection> collectionsToAdd)
                            {
                                if (collectionList == null)
                                    throw new Exception("collectionList should not be null!");
                                collectionsToAdd ??= new List<LoadedCollection>();

                                foreach (var loadedCollection in collectionsToAdd)
                                {
                                    var newVltCollection = new VltCollection(newVault, vltClass, loadedCollection.Name);

                                    foreach (var (key, value) in loadedCollection.Data)
                                    {
                                        if (!vltClass.TryGetField(key, out var field))
                                        {
                                            throw new SerializedDatabaseLoaderException(
                                                $"Cannot find field: {vltClass.Name}/{key}");
                                        }

                                        newVltCollection.SetRawValue(key,
                                            ConvertSerializedValueToDataValue(_database.Options.GameId, vaultDirectory,
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
                                {
                                    throw new SerializedDatabaseLoaderException(
                                        $"Duplicate collection found! Multiple collections at '{newCollection.ShortPath}' have been defined in your YML files.");
                                }

                                collectionsToBeAdded.Add(newCollection);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("WARN: vault {0} has no folder; looked for {1}", vault, vaultDirectory);
                    }

                    vaultsToSaveDictionary[file.Name].Add(newVault);
                    _database.Vaults.Add(newVault);

                    file.LoadedVaults.Add(newVault);
                }
            }

            // dependency resolution
            var resolved = new List<VaultDependencyNode>();
            var unresolved = new List<VaultDependencyNode>();

            foreach (var vault in _database.Vaults)
            {
                var vaultCollections = collectionsToBeAdded.Where(c => c.Vault.Name == vault.Name).ToList();
                VaultDependencyNode node = new VaultDependencyNode(vault);

                foreach (var vaultCollection in vaultCollections)
                {
                    string parentKey = collectionParentDictionary[vaultCollection.ShortPath];

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

            if (unresolved.Count != 0)
            {
                throw new SerializedDatabaseLoaderException("Cannot continue loading - unresolved vault dependencies");
            }

            foreach (var node in resolved)
            {
                var vault = node.Vault;
                var vaultCollections = collectionsToBeAdded.Where(c => c.Vault.Name == vault.Name).ToList();

                Debug.WriteLine("Loading collections for vault {0} ({1})", vault.Name, vaultCollections.Count);

                foreach (var collection in vaultCollections)
                {
                    string parentKey = collectionParentDictionary[collection.ShortPath];

                    if (string.IsNullOrEmpty(parentKey))
                    {
                        // Add collection directly
                        _database.RowManager.AddCollection(collection);
                    }
                    else
                    {
                        var parentCollection = collectionDictionary[$"{collection.Class.Name}/{parentKey}"];
                        parentCollection.AddChild(collection);
                    }
                }
            }

            _loadedDatabase = loadedDatabase;

            return loadedDatabase;
        }

        /// <summary>
        /// Generates VLT files
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="outputDirectory"></param>
        /// <param name="filesToSave"></param>
        public void GenerateFiles(BaseProfile profile, string outputDirectory, IEnumerable<string> filesToSave = null)
        {
            List<string> fileList = filesToSave == null ? new List<string>() : filesToSave.ToList();

            profile.SaveFiles(_database, outputDirectory,
                fileList.Count > 0
                    ? _loadedDatabase.Files.Where(f => fileList.Contains(f.Name))
                    : _loadedDatabase.Files);
        }

        private void ResolveDependencies(VaultDependencyNode node, List<VaultDependencyNode> resolved,
            List<VaultDependencyNode> unresolved)
        {
            unresolved.Add(node);

            foreach (var edge in node.Edges)
            {
                if (!resolved.Contains(edge))
                {
                    if (unresolved.Contains(edge))
                    {
                        throw new SerializedDatabaseLoaderException("circular vault dependency!");
                    }

                    ResolveDependencies(edge, resolved, unresolved);
                }
            }

            resolved.Add(node);
            unresolved.Remove(node);
        }

        private VLTBaseType ConvertSerializedValueToDataValue(string gameId, string dir, VltClass vltClass,
            VltClassField field,
            VltCollection vltCollection, object serializedValue, bool createInstance = true)
        {
            //    0. Is it null? Bail out right away.
            //    1. Is it a string? Determine underlying primitive type, and then convert.
            //    2. Is it a list? Ensure we have an array, and then convert all values RECURSIVELY.
            //    3. Is it a dictionary? Convert and set all values RECURSIVELY, ignoring ones that cannot be set at runtime.
            //    4. Are none of those conditions true? Bail out.

            if (serializedValue == null)
            {
                throw new InvalidDataException("Null serializedValue is NOT PERMITTED!");
            }

            // Create a new data instance
            var instance = createInstance
                ? TypeRegistry.CreateInstance(_database.Options.GameId, vltClass, field, vltCollection)
                : TypeRegistry.ConstructInstance(TypeRegistry.ResolveType(gameId, field.TypeName), vltClass, field,
                    vltCollection);

            return DoValueConversion(gameId, dir, vltClass, field, vltCollection, serializedValue, instance);
        }

        private VLTBaseType DoValueConversion(string gameId, string dir, VltClass vltClass, VltClassField field,
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
                            {
                                throw new InvalidDataException(
                                    $"Could not locate blob data file for {vltCollection.ShortPath}[{field.Name}]");
                            }

                            blob.Data = File.ReadAllBytes(str);

                            return blob;
                        }
                    }

                    break;
                case Dictionary<object, object> dictionary:
                    return (VLTBaseType) (instance is VLTArrayType array
                        ? DoArrayConversion(gameId, dir, vltClass, field, vltCollection, array, dictionary)
                        : DoDictionaryConversion(gameId, dir, vltClass, field, vltCollection, instance, dictionary));
            }

            throw new InvalidDataException("Could not convert serialized value of type: " + serializedValue.GetType());
        }

        private VLTArrayType DoArrayConversion(string gameId, string dir, VltClass vltClass, VltClassField field,
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
                    ConvertSerializedValueToDataValue(gameId, dir, vltClass, field, vltCollection, o, false);

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
                {
                    throw new InvalidDataException(
                        $"Cannot set unknown property of '{instance.GetType()}': '{propName}'");
                }

                if (propertyInfo.SetMethod == null || !propertyInfo.SetMethod.IsPublic)
                {
                    continue;
                }

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
                    {
                        if (elemType.IsEnum)
                        {
                            newList[index] = Enum.Parse(elemType, objects[index].ToString());
                        }
                        else
                        {
                            if (elemType == typeof(string))
                                newList[index] = objects[index];
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
            {
                if (s.StartsWith("0x") && elemType == typeof(uint))
                {
                    return uint.Parse(s.Substring(2), NumberStyles.AllowHexSpecifier);
                }
            }

            return value;
        }
    }
}