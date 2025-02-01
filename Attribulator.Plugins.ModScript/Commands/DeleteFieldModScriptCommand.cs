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

            var fieldKey = databaseHelper.StringToKey(FieldName);

            if (!collection.HasEntry(fieldKey))
            {
                throw new CommandExecutionException(
                    $"Field {FieldName} not found in collection {ClassName}/{CollectionName}");
            }

            collection.RemoveValue(fieldKey);
            databaseHelper.MarkVaultAsModified(collection.Vault);
        }
    }
}