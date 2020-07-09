using System;
using System.Linq;
using System.Threading.Tasks;
using Attribulator.API.Plugin;
using Attribulator.ModScript.API;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Attribulator.Plugins.ModScript
{
    [Verb("script-commands", HelpText = "List the available ModScript commands.")]
    public class AvailableCommandsCommand : BaseCommand
    {
        private ILogger<AvailableCommandsCommand> _logger;

        public override void SetServiceProvider(IServiceProvider serviceProvider)
        {
            base.SetServiceProvider(serviceProvider);

            _logger = serviceProvider.GetRequiredService<ILogger<AvailableCommandsCommand>>();
        }

        public override Task<int> Execute()
        {
            var modScriptService = ServiceProvider.GetRequiredService<IModScriptService>();
            var commandNames = modScriptService.GetAvailableCommandNames().ToList();

            _logger.LogInformation("ModScript Commands ({NumCommands}):", commandNames.Count);
            foreach (var commandName in commandNames)
                _logger.LogInformation("{Name}", commandName);

            return Task.FromResult(0);
        }
    }
}