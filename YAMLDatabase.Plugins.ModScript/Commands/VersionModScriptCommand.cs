using System.Collections.Generic;

namespace YAMLDatabase.Plugins.ModScript.Commands
{
    public class VersionModScriptCommand : BaseModScriptCommand
    {
        public string Version { get; private set; }

        public override void Parse(List<string> parts)
        {
            Version = parts[1];

            if (Version != "4.6")
                throw new ModScriptParserException(
                    "This tool is only compatible with ModScript files for NFS-VltEd 4.6.");
        }

        public override void Execute(ModScriptDatabaseHelper database)
        {
            //
        }
    }
}