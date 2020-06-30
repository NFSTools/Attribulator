using YAMLDatabase.API.Plugin;

namespace YAMLDatabase.Plugins.CoreCommands
{
    /// <summary>
    ///     Base class for the Core Commands plugin.
    /// </summary>
    public class CoreCommandsPlugin : IPlugin
    {
        public string GetName()
        {
            return "Core Commands";
        }
    }
}