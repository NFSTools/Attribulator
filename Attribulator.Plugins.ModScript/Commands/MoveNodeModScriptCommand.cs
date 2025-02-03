using System.Collections.Generic;
using Attribulator.ModScript.API;
using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;

namespace Attribulator.Plugins.ModScript.Commands
{
    // move_node class node [parent]
    public class MoveNodeModScriptCommand : BaseModScriptCommand, IParseableModScriptCommand<MoveNodeModScriptCommand>
    {
        public required string ClassName { get; init; }
        public required string CollectionName { get; init; }
        public required string? ParentName { get; init; }

        public static MoveNodeModScriptCommand Parse(List<string> parts)
        {
            if (parts.Count is < 3 or > 4)
                throw new CommandParseException("Expected command to be in format: move_node class node [parent]");

            var className = parts[1];
            var collectionName = parts[2];
            var parentName = parts.Count == 4 ? parts[3] : null;

            if (parentName == collectionName)
                throw new CommandParseException("Parent name cannot be the same as collection name.");
            return new MoveNodeModScriptCommand
            {
                ClassName = className,
                CollectionName = collectionName,
                ParentName = parentName,
            };
        }

        protected override void Execute<TKey>(DatabaseHelper<TKey> databaseHelper)
        {
            var collectionToMove = GetCollection(databaseHelper, ClassName, CollectionName)!;
            VltCollection<TKey>? newParentCollection = null;

            if (ParentName != null)
            {
                newParentCollection = GetCollection(databaseHelper, ClassName, ParentName)!;

                if (IsChild(collectionToMove, newParentCollection))
                    throw new CommandExecutionException(
                        $"Requested parent collection {ParentName} is a child of {CollectionName}.");
            }

            // Did the parent change?
            if (ReferenceEquals(newParentCollection, collectionToMove.Parent)) return;

            var oldVault = collectionToMove.Vault;

            if (newParentCollection == null)
            {
                collectionToMove.Parent!.RemoveChild(collectionToMove);
            }
            else
            {
                newParentCollection.AddChild(collectionToMove);
            }

            databaseHelper.MarkVaultAsModified(oldVault);
            if (oldVault != collectionToMove.Vault)
                databaseHelper.MarkVaultAsModified(collectionToMove.Vault);
        }

        private static bool IsChild<TKey>(VltCollection<TKey> root, VltCollection<TKey> possibleChild)
            where TKey : struct, IKey<TKey>
        {
            var parent = possibleChild.Parent;

            while (parent != null)
            {
                if (parent == root)
                    return true;
                parent = parent.Parent;
            }

            return false;
        }
    }
}