using System.Collections.Generic;
using System.Linq;
using Attribulator.API.Utils;
using Attribulator.ModScript.API;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.Types;

namespace Attribulator.Plugins.ModScript.Commands
{
    // add_node class parentNode nodeName
    public class AddNodeModScriptCommand : BaseModScriptCommand, IParseableModScriptCommand<AddNodeModScriptCommand>
    {
        public string ClassName { get; set; }
        public string ParentCollectionName { get; set; }
        public string CollectionName { get; set; }

        public static AddNodeModScriptCommand Parse(List<string> parts)
        {
            if (parts.Count != 3 && parts.Count != 4)
                throw new CommandParseException($"3 or 4 tokens expected, got {parts.Count}");

            var className = CleanHashString(parts[1]);
            var parentCollectionName = parts.Count == 4 ? CleanHashString(parts[2]) : "";
            var collectionName = CleanHashString(parts[^1]);

            return new AddNodeModScriptCommand
            {
                ClassName = className,
                ParentCollectionName = parentCollectionName,
                CollectionName = collectionName,
            };
        }

        protected override void Execute<TKey>(DatabaseHelper<TKey> databaseHelper)
        {
            VltCollection<TKey> parentCollection = null;
            if (!string.IsNullOrEmpty(ParentCollectionName))
                if ((parentCollection = GetCollection(databaseHelper, ClassName, ParentCollectionName, false)) == null)
                    throw new CommandExecutionException(
                        $"add_node failed because parent collection does not exist: {ClassName}/{ParentCollectionName}");

            if (GetCollection(databaseHelper, ClassName, CollectionName, false) != null)
                throw new CommandExecutionException(
                    $"add_node failed because collection already exists: {ClassName}/{CollectionName}");

            Vault<TKey> addToVault;

            if (parentCollection != null)
                addToVault = parentCollection.Vault;
            else
                addToVault = databaseHelper.Vaults.FirstOrDefault(vault =>
                    databaseHelper.GetCollectionsInVault(vault)
                        .Any(collection => collection.Class.Key == KeyUtils.StringToKey<TKey>(ClassName)));

            if (addToVault == null)
                throw new CommandExecutionException("failed to determine vault to insert new collection into");

            var newNode = databaseHelper.AddCollection(addToVault, ClassName, CollectionName, parentCollection);
            var vltClass = newNode.Class;

            if (parentCollection != null)
                databaseHelper.CopyCollection(databaseHelper.Database, parentCollection, newNode);
            else
                foreach (var baseField in vltClass.BaseFields)
                {
                    var vltBaseType = FieldUtils.CreateFieldValue(databaseHelper.Database.TypeRegistry, baseField);

                    if (vltBaseType is VltArrayType<TKey> array)
                    {
                        array.Capacity = baseField.MaxCount;
                        for (var i = 0; i < array.Capacity; i++)
                            array.Items.Add(FieldUtils.ConstructFieldType(databaseHelper.Database.TypeRegistry,
                                baseField));
                    }

                    newNode.SetRawValue(baseField.Key,
                        vltBaseType);
                }

            if (vltClass.HasField("CollectionName")) newNode.SetRawValue("CollectionName", CollectionName);
        }
    }
}