using System.Collections.Generic;
using System.IO;
using System.Linq;
using VaultLib.Core.Data;
using VaultLib.Core.DB;

namespace YAMLDatabase.ModScript.Commands
{
    public class DeleteNodeModScriptCommand : BaseModScriptCommand
    {
        public string ClassName { get; set; }
        public string CollectionName { get; set; }

        public override void Parse(List<string> parts)
        {
            if (parts.Count != 3)
            {
                throw new ModScriptParserException($"Expected 3 tokens, got {parts.Count}");
            }

            ClassName = CleanHashString(parts[1]);
            CollectionName = CleanHashString(parts[2]);
        }

        public override void Execute(Database database)
        {
            VltCollection collection = GetCollection(database, ClassName, CollectionName);

            // Disassociate children
            foreach (var vltCollection in collection.Children.ToList())
            {
                collection.RemoveChild(vltCollection);
            }

            if (collection.Parent != null)
            {
                collection.Parent.RemoveChild(collection);
            }
            else
            {
                database.RowManager.RemoveCollection(collection);
            }
        }
    }
}