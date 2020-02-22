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
using VaultLib.Core.Types.EA.Reflection;
using YAMLDatabase.ModScript.Utils;
using YAMLDatabase.Profiles;
using YamlDotNet.Serialization;

namespace YAMLDatabase
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

            foreach (var loadedDatabaseClass in loadedDatabase.Classes)
            {
                var vltClass = new VltClass(loadedDatabaseClass.Name);

                foreach (var loadedDatabaseClassField in loadedDatabaseClass.Fields)
                {
                    var field = new VltClassField(
                        VLT32Hasher.Hash(loadedDatabaseClassField.Name),
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
                        field.StaticValue = ConvertSerializedValueToDataValue(_database.Options.GameId, vltClass, field, null,
                            loadedDatabaseClassField.StaticValue);
                    }

                    vltClass.Fields.Add(field.Key, field);
                }

                _database.AddClass(vltClass);
            }

            foreach (var loadedDatabaseType in loadedDatabase.Types)
            {
                _database.Types.Add(new DatabaseTypeInfo { Name = loadedDatabaseType.Name, Size = loadedDatabaseType.Size });
            }

            var collectionParentDictionary = new Dictionary<VltCollection, string>();
            var collectionDictionary = new Dictionary<string, VltCollection>();
            var vaultsToSaveDictionary = new Dictionary<string, List<Vault>>();

            foreach (var file in loadedDatabase.Files)
            {
                file.LoadedVaults = new List<Vault>();

                var baseDirectory = Path.Combine(_inputDirectory, file.Group, file.Name);
                vaultsToSaveDictionary[file.Name] = new List<Vault>();
                foreach (var vault in file.Vaults)
                {
                    var vaultDirectory = Path.Combine(baseDirectory, vault).Trim();
                    var newVault = new Vault(vault) { Database = _database, IsPrimaryVault = vault == "db" };
                    if (Directory.Exists(vaultDirectory))
                    {

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

                                foreach (var k in loadedCollection.Data.Keys.ToList().Where(k => loadedCollection.Data[k] == null))
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
                                        newVltCollection.SetRawValue(key,
                                            ConvertSerializedValueToDataValue(_database.Options.GameId, vltClass, vltClass[key],
                                                newVltCollection, value));
                                    }

                                    collectionParentDictionary[newVltCollection] = loadedCollection.ParentName;
                                    collectionList.Add(newVltCollection);
                                    collectionDictionary[newVltCollection.ShortPath] = newVltCollection;
                                }
                            }

                            AddCollectionsToList(newCollections, collections);

                            foreach (var newCollection in newCollections)
                            {
                                _database.RowManager.AddCollection(newCollection);
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("WARN: vault {0} has no folder", new object[] { vault });
                    }

                    vaultsToSaveDictionary[file.Name].Add(newVault);
                    _database.Vaults.Add(newVault);

                    file.LoadedVaults.Add(newVault);
                }
            }

            // Resolve hierarchy
            var rowManagerRows = _database.RowManager.Rows;

            for (int i = rowManagerRows.Count - 1; i >= 0; i--)
            {
                VltCollection collection = rowManagerRows[i];
                string parentName = collectionParentDictionary[collection];

                if (parentName != null)
                {
                    VltCollection parentCollection = collectionDictionary[$"{collection.Class.Name}/{parentName}"];
                    parentCollection.AddChild(collection);
                    rowManagerRows.RemoveAt(i);
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
        public void GenerateFiles(BaseProfile profile, string outputDirectory)
        {
            profile.SaveFiles(_database, outputDirectory, _loadedDatabase.Files);
        }

        private VLTBaseType ConvertSerializedValueToDataValue(string gameId, VltClass vltClass, VltClassField field,
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

            return DoValueConversion(gameId, vltClass, field, vltCollection, serializedValue, instance);
        }

        private VLTBaseType DoValueConversion(string gameId, VltClass vltClass, VltClassField field, VltCollection vltCollection,
            object serializedValue, object instance)
        {
            if (serializedValue is string str &&
                instance is PrimitiveTypeBase primitiveTypeBase)
            {
                return ValueConversionUtils.DoPrimitiveConversion(primitiveTypeBase, str);
            }

            if (serializedValue is Dictionary<object, object> dictionary)
            {
                return (VLTBaseType)(instance is VLTArrayType array
                    ? DoArrayConversion(gameId, vltClass, field, vltCollection, array, dictionary)
                    : DoDictionaryConversion(gameId, vltClass, field, vltCollection, instance, dictionary));
            }

            throw new InvalidDataException("Could not convert serialized value of type: " + serializedValue.GetType());
        }

        private VLTArrayType DoArrayConversion(string gameId, VltClass vltClass, VltClassField field,
            VltCollection vltCollection, VLTArrayType array, Dictionary<object, object> dictionary)
        {
            var capacity = ushort.Parse(dictionary["Capacity"].ToString());
            var rawItemList = (List<object>)dictionary["Data"];

            array.Capacity = capacity;
            array.Items = new List<VLTBaseType>();
            array.ItemAlignment = field.Alignment;
            array.FieldSize = field.Size;

            foreach (var o in rawItemList)
            {
                var newArrayItem = ConvertSerializedValueToDataValue(gameId, vltClass, field, vltCollection, o, false);

                array.Items.Add(newArrayItem);
            }

            return array;
        }

        private object DoDictionaryConversion(string gameId, VltClass vltClass, VltClassField field,
            VltCollection vltCollection, object instance, Dictionary<object, object> dictionary)
        {
            foreach (var pair in dictionary)
            {
                var propName = (string)pair.Key;
                var propertyInfo = instance.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);

                if (propertyInfo == null)
                {
                    throw new InvalidDataException($"Cannot set unknown property of '{instance.GetType()}': '{propName}'");
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
                    propertyInfo.SetValue(instance, Convert.ChangeType(pair.Value, propType, CultureInfo.InvariantCulture));
                }
                else if (pair.Value is List<object> objects)
                {
                    var newList = (IList)Activator.CreateInstance(propType, objects.Count);
                    var elemType = propType.GetElementType() ?? throw new Exception();

                    for (var index = 0; index < objects.Count; index++)
                    {
                        if (elemType.IsEnum)
                        {
                            newList[index] = Enum.Parse(elemType, objects[index].ToString());
                        }
                        else
                        {
                            newList[index] = Convert.ChangeType(objects[index].ToString(), elemType, CultureInfo.InvariantCulture);
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
                        DoDictionaryConversion(gameId, vltClass, field, vltCollection, propInstance, objectDictionary));
                }
                else if (pair.Value != null)
                {
                    throw new Exception();
                }
            }

            return instance;
        }
    }
}