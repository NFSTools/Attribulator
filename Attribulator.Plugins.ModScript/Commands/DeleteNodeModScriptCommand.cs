using System.Collections.Generic;
using Attribulator.ModScript.API;

namespace Attribulator.Plugins.ModScript.Commands
{
    public class DeleteNodeModScriptCommand : BaseModScriptCommand
    {
        public string ClassName { get; set; }
        public string CollectionName { get; set; }

        public override void Parse(List<string> parts)
        {
            if (parts.Count != 3) throw new CommandParseException($"Expected 3 tokens, got {parts.Count}");

            ClassName = CleanHashString(parts[1]);
            CollectionName = CleanHashString(parts[2]);
        }

        public override void Execute(DatabaseHelper databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, CollectionName);

            databaseHelper.RemoveCollection(collection).ForEach(RemoveCollectionFromCache);
        }
    }
}