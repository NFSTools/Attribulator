using System;
using Microsoft.Extensions.DependencyInjection;
using YAMLDatabase.API.Plugin;

namespace YAMLDatabase.Plugins.YAMLSupport
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
            return "YAMLDatabase.Plugins.YAMLSupport";
        }
    }
}