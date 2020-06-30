using System.Collections.Generic;
using YAMLDatabase.API.Plugin;
using YAMLDatabase.API.Services;

namespace YAMLDatabase.CLI.Services
{
    public class PluginServiceImpl : IPluginService
    {
        private readonly List<IPluginFactory> _pluginFactories = new List<IPluginFactory>();

        public void RegisterPlugin(IPluginFactory pluginFactory)
        {
            _pluginFactories.Add(pluginFactory);
        }

        public IEnumerable<IPluginFactory> GetPlugins()
        {
            return _pluginFactories;
        }
    }
}