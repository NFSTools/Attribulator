using System.Collections.Generic;
using Attribulator.ModScript.API;

namespace Attribulator.Plugins.ModScript.Commands
{
    // copy_overwrite class sourceNode parentNode nodeName
    public class CopyOverwriteModScriptCommand : CopyNodeModScriptCommand,
        IParseableModScriptCommand<CopyOverwriteModScriptCommand>
    {
        public new static CopyOverwriteModScriptCommand Parse(List<string> parts)
        {
            var copyNode = CopyNodeModScriptCommand.Parse(parts);

            return new CopyOverwriteModScriptCommand
            {
                ClassName = copyNode.ClassName,
                DestinationCollectionName = copyNode.DestinationCollectionName,
                SourceCollectionName = copyNode.SourceCollectionName,
                ParentCollectionName = copyNode.ParentCollectionName,
            };
        }

        protected override void Execute<TKey>(DatabaseHelper<TKey> databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, DestinationCollectionName, false);
            if (collection != null) databaseHelper.RemoveCollection(collection).ForEach(RemoveCollectionFromCache);

            base.Execute(databaseHelper);
        }
    }
}