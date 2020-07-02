using System.Collections.Generic;
using VaultLib.Core.Hashing;

namespace YAMLDatabase.Plugins.ModScript.Commands
{
    // delete_field class node field
    public class DeleteFieldModScriptCommand : BaseModScriptCommand
    {
        public string ClassName { get; set; }
        public string CollectionName { get; set; }
        public string FieldName { get; set; }

        public override void Parse(List<string> parts)
        {
            if (parts.Count != 4) throw new ModScriptParserException($"Expected 4 tokens, got {parts.Count}");

            ClassName = CleanHashString(parts[1]);
            CollectionName = CleanHashString(parts[2]);
            FieldName = CleanHashString(parts[3]);
        }

        public override void Execute(ModScriptDatabaseHelper databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, CollectionName);
            if (collection.HasEntry(FieldName))
            {
                collection.RemoveValue(FieldName);
            }
            else
            {
                var hashed = $"0x{VLT32Hasher.Hash(FieldName):X8}";

                if (collection.HasEntry(hashed))
                    collection.RemoveValue(hashed);
                else
                    throw new ModScriptCommandExecutionException(
                        $"Could not delete field: {ClassName}/{CollectionName}[{FieldName}]");
            }
        }
    }
}