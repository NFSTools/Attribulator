using System.Collections.Generic;
using Attribulator.ModScript.API;
using VaultLib.Core.Hashing;

namespace Attribulator.Plugins.ModScript.Commands
{
    // delete_field class node field
    public class DeleteFieldModScriptCommand : BaseModScriptCommand,
        IParseableModScriptCommand<DeleteFieldModScriptCommand>
    {
        public string ClassName { get; set; }
        public string CollectionName { get; set; }
        public string FieldName { get; set; }

        public static DeleteFieldModScriptCommand Parse(List<string> parts)
        {
            if (parts.Count != 4) throw new CommandParseException($"Expected 4 tokens, got {parts.Count}");

            return new DeleteFieldModScriptCommand
            {
                ClassName = (parts[1]),
                CollectionName = (parts[2]),
                FieldName = (parts[3])
            };
        }

        protected override void Execute<TKey>(DatabaseHelper<TKey> databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, CollectionName);

            bool removedAnything = false;

            if (collection.HasEntry(FieldName))
            {
                collection.RemoveValue(FieldName);
                databaseHelper.MarkVaultAsModified(collection.Vault);
                removedAnything = true;
            }
            else
            {
                var hashed = $"0x{Vlt32Hasher.Hash(FieldName):X8}";

                if (collection.HasEntry(hashed))
                {
                    collection.RemoveValue(hashed);
                    databaseHelper.MarkVaultAsModified(collection.Vault);
                    removedAnything = true;
                }
            }

            if (!removedAnything)
            {
                throw new CommandExecutionException(
                    $"Field {FieldName} not found in collection {ClassName}/{CollectionName}");
            }
        }
    }
}