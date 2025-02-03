using System.Collections.Generic;
using Attribulator.ModScript.API;

namespace Attribulator.Plugins.ModScript.Commands
{
    // rename_node class node name
    public class RenameNodeModScriptCommand : BaseModScriptCommand,
        IParseableModScriptCommand<RenameNodeModScriptCommand>
    {
        public required string ClassName { get; init; }
        public required string CollectionName { get; init; }
        public required string NewName { get; init; }

        public static RenameNodeModScriptCommand Parse(List<string> parts)
        {
            if (parts.Count != 4) throw new CommandParseException($"Expected 4 tokens, got {parts.Count}");

            return new RenameNodeModScriptCommand
            {
                ClassName = parts[1],
                CollectionName = parts[2],
                NewName = parts[3]
            };
        }

        protected override void Execute<TKey>(DatabaseHelper<TKey> databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, CollectionName)!;

            if (GetCollection(databaseHelper, ClassName, NewName, false) != null)
                throw new CommandExecutionException(
                    $"rename_node failed because there is already a collection called '{NewName}'");

            RemoveCollectionFromCache(collection);
            databaseHelper.RenameCollection(collection, NewName);
        }
    }
}