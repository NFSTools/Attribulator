using System.Collections.Generic;
using YAMLDatabase.API.Plugin;

namespace YAMLDatabase.API.Services
{
    /// <summary>
    ///     Exposes an interface for registering and retrieving plugins.
    /// </summary>
    public interface IPluginService
    {
        /// <summary>
        ///     Registers a new plugin.
        /// </summary>
        /// <param name="pluginFactory">The plugin to register.</param>
        void RegisterPlugin(IPluginFactory pluginFactory);

        /// <summary>
        ///     Gets the registered plugins.
        /// </summary>
        /// <returns>The registered plugins.</returns>
        IEnumerable<IPluginFactory> GetPlugins();
    }
}