using Microsoft.Extensions.DependencyInjection;
using YAMLDatabase.API.Plugin;

namespace YAMLDatabase.Plugins.CoreCommands
{
    /// <summary>
    ///     Base class for the core commands plugin.
    /// </summary>
    public class CoreCommandsPlugin : IPluginFactory
    {
        public void Configure(IServiceCollection services)
        {
            services.AddTransient<UnpackCommand>();
            services.AddTransient<PackCommand>();
        }

        public string GetName()
        {
            return "Core Commands";
        }
    }
}