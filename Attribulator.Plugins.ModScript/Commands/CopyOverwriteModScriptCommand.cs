using Attribulator.ModScript.API;

namespace Attribulator.Plugins.ModScript.Commands
{
    // copy_overwrite class sourceNode parentNode nodeName
    public class CopyOverwriteModScriptCommand : CopyNodeModScriptCommand
    {
        public override void Execute(DatabaseHelper databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, DestinationCollectionName, false);
            if (collection != null) databaseHelper.RemoveCollection(collection).ForEach(RemoveCollectionFromCache);

            base.Execute(databaseHelper);
        }
    }
}