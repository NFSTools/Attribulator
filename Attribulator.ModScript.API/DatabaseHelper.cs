using System.Collections.Generic;
using System.Data;
using System.Linq;
using Attribulator.API.Utils;
using Attribulator.ModScript.API.Utils;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.DB;

namespace Attribulator.ModScript.API
{
    public class DatabaseHelper<TKey> where TKey : struct, IKey<TKey>
    {
        private readonly Dictionary<VltUtils.FieldIdentifier<TKey>, VltClassField<TKey>>
            _fieldCache = new();

        private readonly Dictionary<Vault<TKey>, bool> _vaultsModified = new Dictionary<Vault<TKey>, bool>();

        public DatabaseHelper(Database<TKey> database)
        {
            Database = database;
            Collections = database.RowManager.GetCollections()
                .ToDictionary(VltUtils.CreateCollectionIdentifier, c => c);
            database.Vaults.ForEach(v => _vaultsModified[v] = false);
        }

        public Dictionary<VltUtils.CollectionIdentifier<TKey>, VltCollection<TKey>> Collections { get; }
        public Database<TKey> Database { get; }
        public List<Vault<TKey>> Vaults => Database.Vaults;

        public TKey StringToKey(string text, bool register = false)
        {
            return KeyUtils.StringToKey<TKey>(text, register);
        }

        public VltCollection<TKey> FindCollectionByName(string className, string collectionName)
        {
            return Collections.GetValueOrDefault(new VltUtils.CollectionIdentifier<TKey>(
                StringToKey(className),
                StringToKey(collectionName)));
        }

        public IEnumerable<VltCollection<TKey>> GetCollectionsInVault(Vault<TKey> vault)
        {
            return Collections.Values.Where(c => ReferenceEquals(c.Vault, vault));
        }

        public VltCollection<TKey> AddCollection(Vault<TKey> addToVault, string className, string collectionName,
            VltCollection<TKey> parentCollection)
        {
            if (FindCollectionByName(className, collectionName) != null)
                throw new DuplicateNameException(
                    $"A collection in the class '{className}' with the name '{collectionName}' already exists.");

            var collection = new VltCollection<TKey>(addToVault,
                Database.FindClass(StringToKey(className)),
                StringToKey(collectionName, true));
            return AddCollection(collection, parentCollection);
        }

        public VltCollection<TKey> AddCollection(VltCollection<TKey> collection,
            VltCollection<TKey> parentCollection = null)
        {
            Database.RowManager.AddCollection(collection);
            parentCollection?.AddChild(collection);

            Collections[VltUtils.CreateCollectionIdentifier(collection)] = collection;
            MarkVaultAsModified(collection.Vault);
            return collection;
        }

        public void RenameCollection(VltCollection<TKey> collection, string newName)
        {
            var collectionIdentifier = VltUtils.CreateCollectionIdentifier(collection);
            Collections.Remove(collectionIdentifier);
            collection.SetKey(StringToKey(newName));
            if (collection.Class.HasField("CollectionName")) collection.SetRawValue("CollectionName", newName);
            Collections.Add(collectionIdentifier, collection);
            MarkVaultAsModified(collection.Vault);
        }

        public List<VltCollection<TKey>> RemoveCollection(VltCollection<TKey> collection)
        {
            var removed = new List<VltCollection<TKey>> { collection };

            Database.RowManager.RemoveCollection(collection);

            foreach (var child in Database.RowManager.GetCollections(collection.Class.Key)
                         .Where(c => ReferenceEquals(c.Parent, collection))
                         .ToList())
            {
                removed.AddRange(RemoveCollection(child));
            }

            MarkVaultAsModified(collection.Vault);

            return removed;
        }

        public void CopyCollection(Database<TKey> database, VltCollection<TKey> from, VltCollection<TKey> to)
        {
            foreach (var dataPair in from.GetData())
            {
                var field = from.Class[dataPair.Key];
                to.SetRawValue(dataPair.Key,
                    ValueCloningUtils.CloneValue(database, dataPair.Value, to.Class, field, to));
            }

            MarkVaultAsModified(to.Vault);
        }

        public void MarkVaultAsModified(Vault<TKey> vault)
        {
            _vaultsModified[vault] = true;
        }

        public void ChangeVault(VltCollection<TKey> collection, Vault<TKey> newVault)
        {
            var oldVault = collection.Vault;
            collection.SetVault(newVault);
            MarkVaultAsModified(oldVault);
            MarkVaultAsModified(newVault);
        }

        public IEnumerable<string> GetModifiedVaults()
        {
            return _vaultsModified.Where(v => v.Value).Select(v => v.Key.Name);
        }

        /// <summary>
        ///     Finds the field with the given name in the given class.
        /// </summary>
        /// <param name="vltClass">The <see cref="VltClass" /> object to search in.</param>
        /// <param name="fieldName">The field name.</param>
        /// <returns>An instance of the <see cref="VltClassField" /> class.</returns>
        /// <exception cref="CommandExecutionException">if the field cannot be found</exception>
        public VltClassField<TKey> GetField(VltClass<TKey> vltClass, string fieldName)
        {
            if (vltClass == null) throw new CommandExecutionException("GetField() was given a null VltClass!");

            var fieldKey = StringToKey(fieldName);
            var fieldIdentifier = VltUtils.CreateFieldIdentifier(vltClass, fieldKey);
            if (_fieldCache.TryGetValue(fieldIdentifier, out var field)) return field;

            return _fieldCache[fieldIdentifier] = vltClass.FindField(fieldKey);
        }
    }
}