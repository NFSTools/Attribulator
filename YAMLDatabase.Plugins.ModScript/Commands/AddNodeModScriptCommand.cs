using System.Collections.Generic;
using System.IO;
using System.Linq;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.Types;
using YAMLDatabase.ModScript.API;

namespace YAMLDatabase.Plugins.ModScript.Commands
{
    // add_node class parentNode nodeName
    public class AddNodeModScriptCommand : BaseModScriptCommand
    {
        public string ClassName { get; set; }
        public string ParentCollectionName { get; set; }
        public string CollectionName { get; set; }

        public override void Parse(List<string> parts)
        {
            if (parts.Count != 3 && parts.Count != 4)
                throw new CommandParseException($"3 or 4 tokens expected, got {parts.Count}");

            ClassName = CleanHashString(parts[1]);
            ParentCollectionName = parts.Count == 4 ? CleanHashString(parts[2]) : "";
            CollectionName = CleanHashString(parts[^1]);
        }

        public override void Execute(DatabaseHelper databaseHelper)
        {
            VltCollection parentCollection = null;
            if (!string.IsNullOrEmpty(ParentCollectionName))
                if ((parentCollection = GetCollection(databaseHelper, ClassName, ParentCollectionName, false)) == null)
                    throw new InvalidDataException(
                        $"add_node failed because parent collection does not exist: {ClassName}/{ParentCollectionName}");

            if (GetCollection(databaseHelper, ClassName, CollectionName, false) != null)
                throw new InvalidDataException(
                    $"add_node failed because collection already exists: {ClassName}/{CollectionName}");

            Vault addToVault;

            if (parentCollection != null)
                addToVault = parentCollection.Vault;
            else
                addToVault = databaseHelper.Vaults.FirstOrDefault(vault =>
                    databaseHelper.GetCollectionsInVault(vault)
                        .Any(collection => collection.Class.Name == ClassName));

            if (addToVault == null)
                throw new InvalidDataException("failed to determine vault to insert new collection into");

            var newNode = databaseHelper.AddCollection(addToVault, ClassName, CollectionName, parentCollection);

            foreach (var baseField in newNode.Class.BaseFields)
            {
                var vltBaseType = TypeRegistry.CreateInstance(databaseHelper.Database.Options.GameId, newNode.Class,
                    newNode.Class[baseField.Key],
                    newNode);

                if (vltBaseType is VLTArrayType array)
                {
                    array.Capacity = baseField.MaxCount;
                    array.ItemAlignment = baseField.Alignment;
                    array.FieldSize = baseField.Size;
                    array.Items = new List<VLTBaseType>();

                    for (var i = 0; i < array.Capacity; i++)
                        array.Items.Add(TypeRegistry.ConstructInstance(array.ItemType, newNode.Class, baseField,
                            newNode));
                }

                newNode.SetRawValue(baseField.Name,
                    vltBaseType);
            }

            if (newNode.Class.HasField("CollectionName")) newNode.SetDataValue("CollectionName", CollectionName);
        }
    }
}