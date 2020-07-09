using System;
using Attribulator.API.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace Attribulator.Plugins.BPSupport
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
            return "Attribulator.Plugins.BPSupport";
        }
    }
}