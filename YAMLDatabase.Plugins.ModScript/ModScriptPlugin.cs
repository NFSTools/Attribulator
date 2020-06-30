using Microsoft.Extensions.DependencyInjection;
using YAMLDatabase.API.Plugin;

namespace YAMLDatabase.Plugins.ModScript
{
    /// <summary>
    /// Base class for the ModScript plugin.
    /// </summary>
    public class ModScriptPlugin : IPluginFactory
    {
        public void Configure(IServiceCollection services)
        {
            services.AddTransient<ApplyScriptCommand>();
        }

        public string GetName()
        {
            return "ModScript Support";
        }
    }
}