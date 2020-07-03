using YAMLDatabase.API.Plugin;

namespace YAMLDatabase.Plugins.YAMLSupport
{
    public class YamlSupportPlugin : IPlugin
    {
        public string GetName()
        {
            return "YAML Support";
        }

        public void Init()
        {
            //
        }
    }
}