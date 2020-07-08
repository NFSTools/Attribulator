using System;
using Microsoft.Extensions.DependencyInjection;
using YAMLDatabase.API.Plugin;

namespace YAMLDatabase.Plugins.BPSupport
{
    public class BurnoutParadisePluginFactory : IPluginFactory
    {
        public void Configure(IServiceCollection services)
        {
            services.AddTransient<BurnoutParadiseProfile>();
            services.AddTransient<BurnoutParadisePlugin>();
        }

        public IPlugin CreatePlugin(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<BurnoutParadisePlugin>();
        }

        public string GetId()
        {
            return "YAMLDatabase.Plugins.BPSupport";
        }
    }
}