using System.Collections.Generic;
using System.IO;

namespace YAMLDatabase.Plugins.ModScript.Commands
{
    // rename_node class node name
    public class RenameNodeModScriptCommand : BaseModScriptCommand
    {
        public string ClassName { get; set; }
        public string CollectionName { get; set; }
        public string NewName { get; set; }

        public override void Parse(List<string> parts)
        {
            if (parts.Count != 4) throw new ModScriptParserException($"Expected 4 tokens, got {parts.Count}");

            ClassName = parts[1];
            CollectionName = parts[2];
            NewName = parts[3];
        }

        public override void Execute(ModScriptDatabaseHelper databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, CollectionName);

            if (GetCollection(databaseHelper, ClassName, NewName, false) != null)
                throw new InvalidDataException(
                    $"rename_node failed because there is already a collection called '{NewName}'");

            databaseHelper.RenameCollection(collection, NewName);
        }
    }
}