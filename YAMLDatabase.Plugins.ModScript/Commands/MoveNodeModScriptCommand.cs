using System.Collections.Generic;
using System.Linq;
using VaultLib.Core.Data;
using YAMLDatabase.ModScript.API;

namespace YAMLDatabase.Plugins.ModScript.Commands
{
    // move_node class node [parent]
    public class MoveNodeModScriptCommand : BaseModScriptCommand
    {
        public string ClassName { get; set; }
        public string CollectionName { get; set; }
        public string ParentName { get; set; }

        public override void Parse(List<string> parts)
        {
            if (parts.Count < 3 || parts.Count > 4)
                throw new CommandParseException("Expected command to be in format: move_node class node [parent]");

            ClassName = CleanHashString(parts[1]);
            CollectionName = CleanHashString(parts[2]);
            ParentName = parts.Count == 4 ? parts[3] : null;

            if (ParentName == CollectionName)
                throw new CommandParseException("Parent name cannot be the same as collection name.");
        }

        public override void Execute(DatabaseHelper databaseHelper)
        {
            var collectionToMove = GetCollection(databaseHelper, ClassName, CollectionName);
            VltCollection newParentCollection = null;

            if (ParentName != null)
            {
                newParentCollection = GetCollection(databaseHelper, ClassName, ParentName);

                if (IsChild(databaseHelper, collectionToMove, newParentCollection))
                    throw new CommandExecutionException(
                        $"Requested parent collection {ParentName} is a child of {CollectionName}.");
            }

            // Did the parent change?
            if (ReferenceEquals(newParentCollection, collectionToMove.Parent)) return;

            // Disassociated from parent? Add to DB
            if (newParentCollection == null)
            {
                collectionToMove.Parent.RemoveChild(collectionToMove);
                databaseHelper.AddCollection(collectionToMove);
            }
            else
            {
                // Handle new parent
                newParentCollection.AddChild(collectionToMove);
                databaseHelper.Database.RowManager.RemoveCollection(collectionToMove);
            }
        }

        private bool IsChild(DatabaseHelper databaseHelper, VltCollection root, VltCollection test)
        {
            var flattenedChildren =
                databaseHelper.Database.RowManager.EnumerateFlattenedCollections(root.Children);

            return flattenedChildren.Any(child => ReferenceEquals(child, test));
        }
    }
}