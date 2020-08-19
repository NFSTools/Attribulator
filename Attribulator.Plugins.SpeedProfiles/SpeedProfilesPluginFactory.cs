using System;
using Attribulator.API.Plugin;
using Attribulator.Plugins.SpeedProfiles.PlayStation2;
using Microsoft.Extensions.DependencyInjection;

namespace Attribulator.Plugins.SpeedProfiles
{
    public class SpeedProfilesPluginFactory : IPluginFactory
    {
        public void Configure(IServiceCollection services)
        {
            // PC profiles
            services.AddTransient<MostWantedProfile>();
            services.AddTransient<CarbonProfile>();
            services.AddTransient<ProStreetProfile>();
            services.AddTransient<UndercoverProfile>();
            services.AddTransient<WorldProfile>();

            // Console profiles
            services.AddTransient<CarbonProfilePs2>();

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