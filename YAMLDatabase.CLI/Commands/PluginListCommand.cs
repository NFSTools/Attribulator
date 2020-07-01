using System;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using YAMLDatabase.API.Plugin;
using YAMLDatabase.API.Services;

namespace YAMLDatabase.CLI.Commands
{
    [Verb("plugins", HelpText = "List the loaded plugins.")]
    [UsedImplicitly]
    public class PluginListCommand : BaseCommand
    {
        private ILogger<PluginListCommand> _logger;

        public override void SetServiceProvider(IServiceProvider serviceProvider)
        {
            base.SetServiceProvider(serviceProvider);

            _logger = serviceProvider.GetRequiredService<ILogger<PluginListCommand>>();
        }

        public override Task<int> Execute()
        {
            var pluginService = ServiceProvider.GetRequiredService<IPluginService>();
            var plugins = pluginService.GetPlugins().ToList();

            _logger.LogInformation("Plugins ({NumPlugins}):", plugins.Count);
            foreach (var plugin in plugins)
                _logger.LogInformation("{Name} - version {Version}", plugin.GetName(),
                    plugin.GetType().Assembly.GetName().Version);

            return Task.FromResult(0);
        }
    }
}