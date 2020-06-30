using System.Collections.Generic;

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

        public override void Execute(ModScriptDatabaseHelper database)
        {
            throw new ModScriptCommandExecutionException("Cannot execute GenericModScriptCommand.");
        }
    }
}