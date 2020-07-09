using System;
using System.Collections.Generic;
using Attribulator.ModScript.API;
using VaultLib.Core;

namespace Attribulator.Plugins.ModScript.Commands
{
    // change_vault class node vaultName
    public class ChangeVaultModScriptCommand : BaseModScriptCommand
    {
        public string ClassName { get; set; }
        public string CollectionName { get; set; }
        public string VaultName { get; set; }

        public override void Parse(List<string> parts)
        {
            if (parts.Count != 4)
                throw new CommandParseException($"Expected 4 tokens, got {parts.Count} ({string.Join(' ', parts)})");

            ClassName = CleanHashString(parts[1]);
            CollectionName = CleanHashString(parts[2]);
            VaultName = CleanHashString(parts[3]);
        }

        public override void Execute(DatabaseHelper databaseHelper)
        {
            var collection = GetCollection(databaseHelper, ClassName, CollectionName);
            Vault vault;

            try
            {
                vault = databaseHelper.Database.FindVault(VaultName);
            }
            catch (InvalidOperationException e)
            {
                throw new CommandExecutionException($"Cannot find vault '{VaultName}'", e);
            }

            collection.SetVault(vault);
        }
    }
}