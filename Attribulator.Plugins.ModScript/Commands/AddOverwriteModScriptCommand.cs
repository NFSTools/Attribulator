using Attribulator.ModScript.API;

namespace Attribulator.Plugins.ModScript.Commands
{
    // add_overwrite class parentNode nodeName
    public class AddOverwriteModScriptCommand : AddNodeModScriptCommand
    {
        public override void Execute(DatabaseHelper databaseHelper)
        {
            var existingCollection = GetCollection(databaseHelper, ClassName, CollectionName, false);
            if (existingCollection != null)
                databaseHelper.RemoveCollection(existingCollection).ForEach(RemoveCollectionFromCache);

            base.Execute(databaseHelper);
        }
    }
}