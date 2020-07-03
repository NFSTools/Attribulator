using System;
using Microsoft.Extensions.DependencyInjection;
using YAMLDatabase.API.Plugin;
using YAMLDatabase.ModScript.API;

namespace YAMLDatabase.Plugins.ModScript
{
    /// <summary>
    ///     Plugin factory for the ModScript plugin.
    /// </summary>
    public class ModScriptPluginFactory : IPluginFactory
    {
        public void Configure(IServiceCollection services)
        {
            services.AddSingleton<IModScriptService, ModScriptService>();
            services.AddTransient<ApplyScriptCommand>();
            services.AddSingleton<ModScriptPlugin>();
        }

        public IPlugin CreatePlugin(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<ModScriptPlugin>();
        }

        public string GetId()
        {
            return "YAMLDatabase.Plugins.ModScript";
        }
    }
}