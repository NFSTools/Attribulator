using System.Collections.Generic;
using Attribulator.ModScript.API;

namespace Attribulator.Plugins.ModScript.Commands
{
    public class GenericModScriptCommand : BaseModScriptCommand
    {
        public GenericModScriptCommand(string line)
        {
            Command = line;
        }

        public string Command { get; }

        public override void Parse(List<string> parts)
        {
            //
        }

        public override void Execute(DatabaseHelper databaseHelper)
        {
            throw new CommandExecutionException("Cannot execute GenericModScriptCommand.");
        }
    }
}