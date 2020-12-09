using System;
using Attribulator.API.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace Attribulator.Plugins.YAMLSupport
{
    public class YamlSupportPluginFactory : IPluginFactory
    {
        public void Configure(IServiceCollection services)
        {
            services.AddTransient<YamlStorageFormat>();
            services.AddTransient<YamlSupportPlugin>();
        }

        public IPlugin CreatePlugin(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<YamlSupportPlugin>();
        }

        public string GetId()
        {
            return "Attribulator.Plugins.YAMLSupport";
        }
    }
}