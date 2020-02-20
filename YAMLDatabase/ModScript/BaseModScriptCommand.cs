using System.Collections.Generic;

namespace YAMLDatabase.ModScript
{
    public abstract class BaseModScriptCommand
    {
        public abstract void Parse(List<string> parts);
    }
}