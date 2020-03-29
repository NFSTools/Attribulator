using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.DB;

namespace YAMLDatabase.ModScript
{
    public class ModScriptDatabaseHelper
    {
        public Dictionary<string, VltCollection> Collections { get; }
        public Database Database { get; }
        public List<Vault> Vaults => Database.Vaults;

        public ModScriptDatabaseHelper(Database database)
        {
            Database = database;
            Collections = database.RowManager.GetFlattenedCollections().ToDictionary(c => c.ShortPath, c => c);
        }

        public VltCollection FindCollectionByName(string className, string collectionName)
        {
            var key = $"{className}/{collectionName}";
            return Collections.TryGetValue(key, out var collection) ? collection : null;
        }

        public IEnumerable<VltCollection> GetCollectionsInVault(Vault vault)
        {
            return Collections.Values.Where(c => ReferenceEquals(c.Vault, vault));
        }

        public VltCollection AddCollection(Vault addToVault, string className, string collectionName, VltCollection parentCollection)
        {
            if (FindCollectionByName(className, collectionName) != null)
                throw new DuplicateNameException(
                    $"A collection in the class '{className}' with the name '{collectionName}' already exists.");

            var collection = new VltCollection(addToVault, Database.FindClass(className), collectionName);

            if (parentCollection != null)
                // Make the new collection a child of the parent
                parentCollection.AddChild(collection);
            else
                // Just add the collection
                Database.RowManager.Rows.Add(collection);

            Collections[collection.ShortPath] = collection;

            return collection;
        }

        public VltCollection AddCollection(VltCollection collection, VltCollection parentCollection = null)
        {
            if (parentCollection != null)
                parentCollection.AddChild(collection);
            else
                Database.RowManager.Rows.Add(collection);

            Collections[collection.ShortPath] = collection;
            
            return collection;
        }

        public void RenameCollection(VltCollection collection, string newName)
        {
            Collections.Remove(collection.ShortPath);
            collection.SetName(newName);
            if (collection.Class.HasField("CollectionName"))
            {
                collection.SetDataValue("CollectionName", newName);
            }
            Collections.Add(collection.ShortPath, collection);
        }

        public void RemoveCollection(VltCollection collection)
        {
            // Disassociate children
            bool hasParent = collection.Parent != null;
            collection.Parent?.RemoveChild(collection);
            Collections.Remove(collection.ShortPath);

            foreach (var collectionChild in collection.Children.ToList())
            {
                RemoveCollection(collectionChild);
            }

            if (!hasParent)
            {
                Database.RowManager.RemoveCollection(collection);
            }
        }
    }
}