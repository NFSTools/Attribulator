using System.Collections.Generic;
using System.IO;
using System.Linq;
using VaultLib.Core;
using VaultLib.Core.Data;

namespace YAMLDatabase.ModScript.Commands
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
            {
                throw new ModScriptParserException($"3 or 4 tokens expected, got {parts.Count}");
            }

            ClassName = CleanHashString(parts[1]);
            ParentCollectionName = parts.Count == 4 ? CleanHashString(parts[2]) : "";
            CollectionName = CleanHashString(parts[^1]);
        }

        public override void Execute(ModScriptDatabaseHelper database)
        {
            VltCollection parentCollection = null;
            if (!string.IsNullOrEmpty(ParentCollectionName))
            {
                if ((parentCollection = GetCollection(database, ClassName, ParentCollectionName, false)) == null)
                {
                    throw new InvalidDataException($"add_node failed because parent collection does not exist: {ClassName}/{ParentCollectionName}");
                }
            }

            if (GetCollection(database, ClassName, CollectionName, false) != null)
            {
                throw new InvalidDataException($"add_node failed because collection already exists: {ClassName}/{CollectionName}");
            }

            Vault addToVault;

            if (parentCollection != null)
            {
                addToVault = parentCollection.Vault;
            }
            else
            {
                addToVault = database.Vaults.FirstOrDefault(vault =>
                    database.GetCollectionsInVault(vault)
                        .Any(collection => collection.Class.Name == ClassName));
            }

            if (addToVault == null)
            {
                throw new InvalidDataException("failed to determine vault to insert new collection into");
            }

            var newNode = database.AddCollection(addToVault, ClassName, CollectionName, parentCollection);

            foreach (var baseField in newNode.Class.BaseFields)
            {
                newNode.SetRawValue(baseField.Name,
                    TypeRegistry.CreateInstance(database.Database.Options.GameId, newNode.Class, newNode.Class[baseField.Key],
                        newNode));
            }

            if (newNode.Class.HasField("CollectionName"))
            {
                newNode.SetDataValue("CollectionName", CollectionName);
            }
        }
    }
}