using System;
using Microsoft.Extensions.DependencyInjection;
using YAMLDatabase.API.Plugin;

namespace YAMLDatabase.Plugins.SpeedProfiles
{
    public class SpeedProfilesPluginFactory : IPluginFactory
    {
        public void Configure(IServiceCollection services)
        {
            services.AddTransient<MostWantedProfile>();
            services.AddTransient<WorldProfile>();
            services.AddTransient<SpeedProfilesPlugin>();
        }

        public IPlugin CreatePlugin(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetService<SpeedProfilesPlugin>();
        }

        public string GetId()
        {
            return "YAMLDatabase.Plugins.SpeedProfiles";
        }
    }
}