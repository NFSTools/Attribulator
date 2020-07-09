using System;
using System.Collections.Generic;
using Attribulator.API.Plugin;
using Attribulator.API.Services;

namespace Attribulator.CLI.Services
{
    public class PluginServiceImpl : IPluginService
    {
        private readonly List<IPlugin> _plugins = new List<IPlugin>();

        public void RegisterPlugin(IPlugin plugin)
        {
            if (plugin == null) throw new ArgumentNullException(nameof(plugin));

            _plugins.Add(plugin);
        }

        public IEnumerable<IPlugin> GetPlugins()
        {
            return _plugins;
        }
    }
}