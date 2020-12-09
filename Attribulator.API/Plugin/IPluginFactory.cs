using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace Attribulator.API.Plugin
{
    /// <summary>
    ///     Exposes an interface for a plugin factory.
    /// </summary>
    public interface IPluginFactory
    {
        /// <summary>
        ///     Configures the dependency injection container.
        /// </summary>
        /// <param name="services"></param>
        void Configure(IServiceCollection services);

        /// <summary>
        ///     Builds the plugin object.
        /// </summary>
        /// <param name="serviceProvider">The DI service provider.</param>
        /// <returns>The plugin object.</returns>
        IPlugin CreatePlugin(IServiceProvider serviceProvider);

        /// <summary>
        ///     Gets the unique ID of the plugin.
        /// </summary>
        /// <returns>The unique ID of the plugin.</returns>
        string GetId();

        /// <summary>
        ///     Gets the list of IDs of required plugins.
        /// </summary>
        /// <returns>The list of required plugin IDs.</returns>
        IEnumerable<string> GetRequiredPlugins()
        {
            return Array.Empty<string>();
        }
    }
}