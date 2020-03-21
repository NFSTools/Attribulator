using System.Collections.Generic;
using VaultLib.Core.DB;

namespace YAMLDatabase.ModScript.Commands
{
    public class VersionModScriptCommand : BaseModScriptCommand
    {
        public string Version { get; private set; }

        public override void Parse(List<string> parts)
        {
            this.Version = parts[1];

            if (this.Version != "4.6")
            {
                throw new ModScriptParserException("This tool is only compatible with ModScript files for NFS-VltEd 4.6.");
            }
        }

        public override void Execute(ModScriptDatabaseHelper database)
        {
            //
        }
    }
}