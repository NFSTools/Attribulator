using System.Collections.Generic;
using Attribulator.ModScript.API;

namespace Attribulator.Plugins.ModScript.Commands
{
    // change_vault class node vaultName
    public class ChangeVaultModScriptCommand : BaseModScriptCommand,
        IParseableModScriptCommand<ChangeVaultModScriptCommand>
    {
        public required string ClassName { get; init; }
        public required string CollectionName { get; init; }
        public required string VaultName { get; init; }

        public static ChangeVaultModScriptCommand Parse(List<string> parts)
        {
            if (parts.Count != 4)
                throw new CommandParseException($"Expected 4 tokens, got {parts.Count} ({string.Join(' ', parts)})");

            var className = parts[1];
            var collectionName = parts[2];
            var vaultName = parts[3];

            return new ChangeVaultModScriptCommand
            {
                ClassName = className,
                CollectionName = collectionName,
                VaultName = vaultName
            };
        }

        protected override void Execute<TKey>(DatabaseHelper<TKey> databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, CollectionName)!;
            var vault = databaseHelper.Database.Vaults.Find(v => v.Name == VaultName);

            if (vault == null) throw new CommandExecutionException($"Cannot find vault: {VaultName}");

            databaseHelper.ChangeVault(collection, vault);
        }
    }
}