using System;
using Attribulator.API.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace Attribulator.Plugins.SpeedProfiles
{
    public class SpeedProfilesPluginFactory : IPluginFactory
    {
        public void Configure(IServiceCollection services)
        {
            services.AddTransient<MostWantedProfile>();
            services.AddTransient<CarbonProfile>();
            services.AddTransient<ProStreetProfile>();
            services.AddTransient<UndercoverProfile>();
            services.AddTransient<WorldProfile>();
            services.AddTransient<SpeedProfilesPlugin>();
        }

        public IPlugin CreatePlugin(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<SpeedProfilesPlugin>();
        }

        public string GetId()
        {
            return "Attribulator.Plugins.SpeedProfiles";
        }
    }
}