using System;
using System.Linq;
using System.Threading.Tasks;
using Attribulator.API.Plugin;
using Attribulator.API.Services;
using CommandLine;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Attribulator.CLI.Commands
{
    [Verb("formats", HelpText = "List the available storage formats.")]
    [UsedImplicitly]
    public class FormatListCommand : BaseCommand
    {
        private ILogger<FormatListCommand> _logger;

        public override void SetServiceProvider(IServiceProvider serviceProvider)
        {
            base.SetServiceProvider(serviceProvider);

            _logger = serviceProvider.GetRequiredService<ILogger<FormatListCommand>>();
        }

        public override Task<int> Execute()
        {
            var storageFormatService = ServiceProvider.GetRequiredService<IStorageFormatService>();
            var storageFormats = storageFormatService.GetStorageFormats().ToList();

            _logger.LogInformation("Storage Formats ({NumFormats}):", storageFormats.Count);
            foreach (var storageFormat in storageFormats)
                _logger.LogInformation("{Name} - ID: {Id}", storageFormat.GetFormatName(),
                    storageFormat.GetFormatId());

            return Task.FromResult(0);
        }
    }
}