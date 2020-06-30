using YAMLDatabase.API.Plugin;

namespace YAMLDatabase.Plugins.ModScript
{
    /// <summary>
    ///     Base class for the ModScript plugin.
    /// </summary>
    public class ModScriptPlugin : IPlugin
    {
        public string GetName()
        {
            return "ModScript Support";
        }
    }
}