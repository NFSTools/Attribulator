using System;
using Microsoft.Extensions.DependencyInjection;
using YAMLDatabase.API.Plugin;

namespace YAMLDatabase.Plugins.ModScript
{
    /// <summary>
    ///     Plugin factory for the ModScript plugin.
    /// </summary>
    public class ModScriptPluginFactory : IPluginFactory
    {
        public void Configure(IServiceCollection services)
        {
            services.AddTransient<ApplyScriptCommand>();
            services.AddTransient<ModScriptPlugin>();
        }

        public IPlugin CreatePlugin(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<ModScriptPlugin>();
        }
    }
}