using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VaultLib.Core.Data;
using VaultLib.Core.DB;

namespace YAMLDatabase.ModScript.Commands
{
    // rename_node class node name
    public class RenameNodeModScriptCommand : BaseModScriptCommand
    {
        public string ClassName { get; set; }
        public string CollectionName { get; set; }
        public string NewName { get; set; }

        public override void Parse(List<string> parts)
        {
            if (parts.Count != 4)
            {
                throw new ModScriptParserException($"Expected 4 tokens, got {parts.Count}");
            }

            ClassName = parts[1];
            CollectionName = parts[2];
            NewName = parts[3];
        }

        public override void Execute(Database database)
        {
            VltCollection collection = GetCollection(database, ClassName, CollectionName);

            if (GetCollection(database, ClassName, NewName, false) != null)
            {
                throw new InvalidDataException($"rename_node failed because there is already a collection called '{NewName}'");
            }

            //Debug.WriteLine("renaming {0} to {1}", CollectionName, NewName);

            collection.SetName(NewName);
        }
    }
}