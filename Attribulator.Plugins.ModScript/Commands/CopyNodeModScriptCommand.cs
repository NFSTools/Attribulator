using System.Collections.Generic;
using Attribulator.API.Utils;
using Attribulator.ModScript.API;
using VaultLib.Core.Data;

namespace Attribulator.Plugins.ModScript.Commands
{
    // copy_node class sourceNode parentNode nodeName
    public class CopyNodeModScriptCommand : BaseModScriptCommand, IParseableModScriptCommand<CopyNodeModScriptCommand>
    {
        public string ClassName { get; set; }
        public string SourceCollectionName { get; set; }
        public string ParentCollectionName { get; set; }
        public string DestinationCollectionName { get; set; }

        public static CopyNodeModScriptCommand Parse(List<string> parts)
        {
            if (parts.Count != 4 && parts.Count != 5)
                throw new CommandParseException($"4 or 5 tokens expected, got {parts.Count}");

            var className = parts[1];
            var sourceCollectionName = parts[2];
            var parentCollectionName = parts.Count == 5 ? parts[3] : "";
            var destinationCollectionName = parts[^1];

            return new CopyNodeModScriptCommand
            {
                ClassName = className,
                SourceCollectionName = sourceCollectionName,
                ParentCollectionName = parentCollectionName,
                DestinationCollectionName = destinationCollectionName,
            };
        }

        protected override void Execute<TKey>(DatabaseHelper<TKey> databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, SourceCollectionName);

            if (collection == null)
                throw new CommandExecutionException(
                    $"copy_node failed because there is no collection called '{SourceCollectionName}'");

            if (databaseHelper.FindCollectionByName(ClassName, DestinationCollectionName) != null)
                throw new CommandExecutionException(
                    $"copy_node failed because there is already a collection called '{DestinationCollectionName}'");

            VltCollection<TKey> parentCollection = null;

            if (!string.IsNullOrWhiteSpace(ParentCollectionName))
            {
                parentCollection = databaseHelper.FindCollectionByName(ClassName, ParentCollectionName);

                if (parentCollection == null)
                    throw new CommandExecutionException(
                        $"copy_node failed because the parent collection called '{ParentCollectionName}' does not exist");
            }

            var newCollection = new VltCollection<TKey>(collection.Vault, collection.Class,
                KeyUtils.StringToKey<TKey>(DestinationCollectionName, true));
            databaseHelper.CopyCollection(databaseHelper.Database, collection, newCollection);

            if (newCollection.Class.HasField("CollectionName"))
                newCollection.SetRawValue("CollectionName", DestinationCollectionName);

            databaseHelper.AddCollection(newCollection, parentCollection);
        }
    }
}