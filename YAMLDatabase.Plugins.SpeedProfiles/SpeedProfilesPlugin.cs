using Microsoft.Extensions.DependencyInjection;
using YAMLDatabase.API.Plugin;

namespace YAMLDatabase.Plugins.SpeedProfiles
{
    public class SpeedProfilesPlugin : IPluginFactory
    {
        public void Configure(IServiceCollection services)
        {
            //
        }

        public string GetName()
        {
            return "Speed Profiles";
        }
    }
}