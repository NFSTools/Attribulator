using System;
using Attribulator.API.Plugin;
using Attribulator.Plugins.SpeedProfiles.PC;
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
            services.AddTransient<Consoles.PlayStation2.CarbonProfile>();
            services.AddTransient<Consoles.X360.CarbonDemoProfile>();
            services.AddTransient<Consoles.X360.MostWantedProfile>();

            services.AddTransient<SpeedProfilesPlugin>();
        }

        public IPlugin CreatePlugin(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<SpeedProfilesPlugin>();
        }

        public string GetId()
        {
            return "Attribulator.Plugins.SpeedProfiles";
        }
    }
}