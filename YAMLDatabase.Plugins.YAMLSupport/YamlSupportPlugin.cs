using Microsoft.Extensions.DependencyInjection;
using YAMLDatabase.API.Plugin;

namespace YAMLDatabase.Plugins.YAMLSupport
{
    /// <summary>
    ///     Base class for the YAML support plugin.
    /// </summary>
    public class YamlSupportPlugin : IPluginFactory
    {
        public void Configure(IServiceCollection services)
        {
            services.AddTransient<YamlStorageFormat>();
        }

        public string GetName()
        {
            return "YAML Support";
        }
    }
}