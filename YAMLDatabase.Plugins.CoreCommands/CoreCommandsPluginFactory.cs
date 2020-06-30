using System;
using Microsoft.Extensions.DependencyInjection;
using YAMLDatabase.API.Plugin;

namespace YAMLDatabase.Plugins.CoreCommands
{
    /// <summary>
    ///     Plugin factory for the Core Commands plugin.
    /// </summary>
    public class CoreCommandsPluginFactory : IPluginFactory
    {
        public void Configure(IServiceCollection services)
        {
            services.AddTransient<UnpackCommand>();
            services.AddTransient<PackCommand>();
            services.AddTransient<CoreCommandsPlugin>();
        }

        public IPlugin CreatePlugin(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<CoreCommandsPlugin>();
        }
    }
}