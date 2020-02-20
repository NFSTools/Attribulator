using System.Collections.Generic;

namespace YAMLDatabase.ModScript
{
    public class VersionModScriptCommand : BaseModScriptCommand
    {
        public string Version { get; private set; }

        public override void Parse(List<string> parts)
        {
            this.Version = parts[1];
        }
    }
}