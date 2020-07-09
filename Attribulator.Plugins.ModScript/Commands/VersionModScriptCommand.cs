using System.Collections.Generic;
using Attribulator.ModScript.API;

namespace Attribulator.Plugins.ModScript.Commands
{
    public class VersionModScriptCommand : BaseModScriptCommand
    {
        public string Version { get; private set; }

        public override void Parse(List<string> parts)
        {
            Version = parts[1];

            if (Version != "4.6")
                throw new CommandParseException(
                    "This tool is only compatible with ModScript files for NFS-VltEd 4.6.");
        }

        public override void Execute(DatabaseHelper databaseHelper)
        {
            //
        }
    }
}