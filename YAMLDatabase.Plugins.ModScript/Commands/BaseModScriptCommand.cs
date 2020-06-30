using System.Collections.Generic;
using System.Globalization;
using VaultLib.Core.Data;
using VaultLib.Core.Hashing;

namespace YAMLDatabase.Plugins.ModScript.Commands
{
    public abstract class BaseModScriptCommand
    {
        public string Line { get; set; }
        public abstract void Parse(List<string> parts);

        public abstract void Execute(ModScriptDatabaseHelper database);

        protected VltCollection GetCollection(ModScriptDatabaseHelper database, string className, string collectionName,
            bool throwOnMissing = true)
        {
            var collection = database.FindCollectionByName(className, collectionName);

            if (collection == null && throwOnMissing)
                throw new ModScriptCommandExecutionException($"Cannot find collection: {className}/{collectionName}");

            return collection;
        }

        protected VltClassField GetField(VltClass vltClass, string fieldName)
        {
            if (vltClass == null) throw new ModScriptCommandExecutionException("GetField() was given a null VltClass!");

            if (vltClass.TryGetField(fieldName, out var field)) return field;

            throw new ModScriptCommandExecutionException($"Cannot find field: {vltClass.Name}[{fieldName}]");
        }

        protected string CleanHashString(string hashString)
        {
            if (hashString.StartsWith("0x"))
                hashString =
                    HashManager.ResolveVLT(uint.Parse(hashString.Substring(2), NumberStyles.AllowHexSpecifier));

            return hashString;
        }
    }
}