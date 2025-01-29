using System.Collections.Generic;
using Attribulator.ModScript.API;

namespace Attribulator.Plugins.ModScript.Commands
{
    // add_overwrite class parentNode nodeName
    public class AddOverwriteModScriptCommand : AddNodeModScriptCommand,
        IParseableModScriptCommand<AddOverwriteModScriptCommand>
    {
        public new static AddOverwriteModScriptCommand Parse(List<string> parts)
        {
            var cmd = AddNodeModScriptCommand.Parse(parts);

            return new AddOverwriteModScriptCommand
            {
                ClassName = cmd.ClassName,
                CollectionName = cmd.CollectionName,
                ParentCollectionName = cmd.ParentCollectionName
            };
        }

        protected override void Execute<TKey>(DatabaseHelper<TKey> databaseHelper)
        {
            var existingCollection = GetCollection(databaseHelper, ClassName, CollectionName, false);
            if (existingCollection != null)
                databaseHelper.RemoveCollection(existingCollection).ForEach(RemoveCollectionFromCache);

            base.Execute(databaseHelper);
        }
    }
}