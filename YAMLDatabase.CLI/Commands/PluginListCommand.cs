using System;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using YAMLDatabase.API.Plugin;
using YAMLDatabase.API.Services;

namespace YAMLDatabase.CLI.Commands
{
    [Verb("plugins", HelpText = "List the loaded plugins.")]
    [UsedImplicitly]
    public class PluginListCommand : BaseCommand
    {
        public override Task<int> Execute()
        {
            var pluginService = ServiceProvider.GetRequiredService<IPluginService>();

            foreach (var plugin in pluginService.GetPlugins()) Console.WriteLine(plugin.GetName());

            return Task.FromResult(0);
        }
    }
}