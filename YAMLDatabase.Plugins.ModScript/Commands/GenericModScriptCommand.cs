using System.Collections.Generic;
using YAMLDatabase.ModScript.API;

namespace YAMLDatabase.Plugins.ModScript.Commands
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