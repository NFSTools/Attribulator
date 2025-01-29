using System.Collections.Generic;
using Attribulator.ModScript.API;

namespace Attribulator.Plugins.ModScript.Commands
{
    // change_vault class node vaultName
    public class ChangeVaultModScriptCommand : BaseModScriptCommand,
        IParseableModScriptCommand<ChangeVaultModScriptCommand>
    {
        public string ClassName { get; set; }
        public string CollectionName { get; set; }
        public string VaultName { get; set; }

        public static ChangeVaultModScriptCommand Parse(List<string> parts)
        {
            if (parts.Count != 4)
                throw new CommandParseException($"Expected 4 tokens, got {parts.Count} ({string.Join(' ', parts)})");

            var className = CleanHashString(parts[1]);
            var collectionName = CleanHashString(parts[2]);
            var vaultName = CleanHashString(parts[3]);

            return new ChangeVaultModScriptCommand
            {
                ClassName = className,
                CollectionName = collectionName,
                VaultName = vaultName
            };
        }

        protected override void Execute<TKey>(DatabaseHelper<TKey> databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, CollectionName);
            var vault = databaseHelper.Database.Vaults.Find(v => v.Name == VaultName);

            if (vault == null) throw new CommandExecutionException($"Cannot find vault: {VaultName}");

            databaseHelper.ChangeVault(collection, vault);
        }
    }
}