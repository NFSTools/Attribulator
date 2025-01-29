using System.Collections.Generic;
using Attribulator.ModScript.API;

namespace Attribulator.Plugins.ModScript.Commands
{
    public class DeleteNodeModScriptCommand : BaseModScriptCommand,
        IParseableModScriptCommand<DeleteNodeModScriptCommand>
    {
        public string ClassName { get; set; }
        public string CollectionName { get; set; }

        public static DeleteNodeModScriptCommand Parse(List<string> parts)
        {
            if (parts.Count != 3) throw new CommandParseException($"Expected 3 tokens, got {parts.Count}");

            return new DeleteNodeModScriptCommand
            {
                ClassName = CleanHashString(parts[1]),
                CollectionName = CleanHashString(parts[2])
            };
        }

        protected override void Execute<TKey>(DatabaseHelper<TKey> databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, CollectionName);

            databaseHelper.RemoveCollection(collection).ForEach(RemoveCollectionFromCache);
        }
    }
}