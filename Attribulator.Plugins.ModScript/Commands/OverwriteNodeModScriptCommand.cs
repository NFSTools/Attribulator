using Attribulator.ModScript.API;

namespace Attribulator.Plugins.ModScript.Commands
{
    // overwrite_node class parentNode nodeName
    public class OverwriteNodeModScriptCommand : AddNodeModScriptCommand
    {
        public override void Execute(DatabaseHelper databaseHelper)
        {
            var existingCollection = GetCollection(databaseHelper, ClassName, CollectionName, false);
            if (existingCollection != null)
            {
                databaseHelper.RemoveCollection(existingCollection);
                RemoveCollectionFromCache(existingCollection);
            }

            base.Execute(databaseHelper);
        }
    }
}