﻿using System.Collections.Generic;
using Attribulator.API.Plugin;

namespace Attribulator.API.Services
{
    /// <summary>
    ///     Exposes an interface for registering and retrieving plugins.
    /// </summary>
    public interface IPluginService
    {
        /// <summary>
        ///     Registers a new plugin.
        /// </summary>
        /// <param name="plugin">The plugin to register.</param>
        void RegisterPlugin(IPlugin plugin);

        /// <summary>
        ///     Gets the registered plugins.
        /// </summary>
        /// <returns>The registered plugins.</returns>
        IEnumerable<IPlugin> GetPlugins();
    }
}