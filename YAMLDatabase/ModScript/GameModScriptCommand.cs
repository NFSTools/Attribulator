using System.Collections.Generic;

namespace YAMLDatabase.ModScript
{
    public class GameModScriptCommand : BaseModScriptCommand
    {
        public string Game { get; private set; }

        public override void Parse(List<string> parts)
        {
            this.Game = parts[1];
        }
    }
}