using System;
using System.Collections.Generic;
using VaultLib.Core;
using VaultLib.Core.Data;

namespace YAMLDatabase.ModScript.Commands
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
            {
                throw new ModScriptParserException($"Expected 4 tokens, got {parts.Count} ({string.Join(' ', parts)})");
            }

            ClassName = CleanHashString(parts[1]);
            CollectionName = CleanHashString(parts[2]);
            VaultName = CleanHashString(parts[3]);
        }

        public override void Execute(ModScriptDatabaseHelper database)
        {
            VltCollection collection = GetCollection(database, ClassName, CollectionName);
            Vault vault;

            try
            {
                vault = database.Database.FindVault(VaultName);
            }
            catch (InvalidOperationException e)
            {
                throw new ModScriptCommandExecutionException($"Cannot find vault '{VaultName}'", e);
            }

            collection.SetVault(vault);
        }
    }
}