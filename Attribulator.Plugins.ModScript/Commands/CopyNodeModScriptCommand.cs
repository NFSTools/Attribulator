using System.Collections.Generic;
using Attribulator.ModScript.API;
using VaultLib.Core.Data;

namespace Attribulator.Plugins.ModScript.Commands
{
    // copy_node class sourceNode parentNode nodeName
    public class CopyNodeModScriptCommand : BaseModScriptCommand
    {
        public string ClassName { get; set; }
        public string SourceCollectionName { get; set; }
        public string ParentCollectionName { get; set; }
        public string DestinationCollectionName { get; set; }

        public override void Parse(List<string> parts)
        {
            if (parts.Count != 4 && parts.Count != 5)
                throw new CommandParseException($"4 or 5 tokens expected, got {parts.Count}");

            ClassName = parts[1];
            SourceCollectionName = parts[2];
            ParentCollectionName = parts.Count == 5 ? parts[3] : "";
            DestinationCollectionName = parts[^1];
        }

        public override void Execute(DatabaseHelper databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, SourceCollectionName);

            if (collection == null)
                throw new CommandExecutionException(
                    $"copy_node failed because there is no collection called '{SourceCollectionName}'");

            if (databaseHelper.FindCollectionByName(ClassName, DestinationCollectionName) != null)
                throw new CommandExecutionException(
                    $"copy_node failed because there is already a collection called '{DestinationCollectionName}'");

            VltCollection parentCollection = null;

            if (!string.IsNullOrWhiteSpace(ParentCollectionName))
            {
                parentCollection = databaseHelper.FindCollectionByName(ClassName, ParentCollectionName);

                if (parentCollection == null)
                    throw new CommandExecutionException(
                        $"copy_node failed because the parent collection called '{ParentCollectionName}' does not exist");
            }

            var newCollection = new VltCollection(collection.Vault, collection.Class, DestinationCollectionName);
            databaseHelper.CopyCollection(databaseHelper.Database, collection, newCollection);

            if (newCollection.Class.HasField("CollectionName"))
                newCollection.SetDataValue("CollectionName", DestinationCollectionName);

            databaseHelper.AddCollection(newCollection, parentCollection);
        }
    }
}