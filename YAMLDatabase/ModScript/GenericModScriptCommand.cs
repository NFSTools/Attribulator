using System.Collections.Generic;

namespace YAMLDatabase.ModScript
{
    public class GenericModScriptCommand : BaseModScriptCommand
    {
        public GenericModScriptCommand(string line)
        {
            this.Command = line;
        }

        public string Command { get; }

        public override void Parse(List<string> parts)
        {
            //
        }
    }
}