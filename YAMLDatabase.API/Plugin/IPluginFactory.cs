using System;
using Microsoft.Extensions.DependencyInjection;

namespace YAMLDatabase.API.Plugin
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
    }
}