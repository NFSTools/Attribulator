using Microsoft.Extensions.DependencyInjection;

namespace YAMLDatabase.API.Plugin
{
    /// <summary>
    /// Exposes an interface for a plugin.
    /// </summary>
    public interface IPluginFactory
    {
        /// <summary>
        /// Configures the dependency injection container.
        /// </summary>
        /// <param name="services"></param>
        void Configure(IServiceCollection services);

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        /// <returns>The name of the plugin.</returns>
        string GetName();
    }
}