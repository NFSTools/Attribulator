using System;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using YAMLDatabase.API.Plugin;
using YAMLDatabase.API.Services;

namespace YAMLDatabase.CLI.Commands
{
    [Verb("formats", HelpText = "List the available storage formats.")]
    [UsedImplicitly]
    public class FormatListCommand : BaseCommand
    {
        public override Task<int> Execute()
        {
            var storageFormatService = ServiceProvider.GetRequiredService<IStorageFormatService>();

            foreach (var storageFormat in storageFormatService.GetStorageFormats()) 
                Console.WriteLine("{0} - ID: {1}", storageFormat.GetFormatName(), storageFormat.GetFormatId());
            return Task.FromResult(0);
        }
    }
}