using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Attribulator.API.Exceptions;
using Attribulator.API.Plugin;
using Attribulator.API.Services;
using CommandLine;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VaultLib.Core.DB;

namespace Attribulator.CLI.Commands
{
    [Verb("generate-hashlist", HelpText = "Generate a hash-source list file from an unpacked database.")]
    public class GenerateHashListCommand : BaseCommand
    {
        private ILogger<GenerateHashListCommand> _logger;

        [Option('i', "input", HelpText = "Directory to read unpacked files from", Required = true)]
        [UsedImplicitly]
        public string InputDirectory { get; set; }

        [Option('o', "output", HelpText = "Path to generated file", Required = true)]
        [UsedImplicitly]
        public string OutputPath { get; set; }

        [Option('p', "profile", HelpText = "The profile to use", Required = true)]
        [UsedImplicitly]
        public string ProfileName { get; set; }

        public override void SetServiceProvider(IServiceProvider serviceProvider)
        {
            base.SetServiceProvider(serviceProvider);

            _logger = ServiceProvider.GetRequiredService<ILogger<GenerateHashListCommand>>();
        }

        public override async Task<int> Execute()
        {
            if (!Directory.Exists(InputDirectory))
                throw new DirectoryNotFoundException($"Cannot find input directory: {InputDirectory}");

            var profile = ServiceProvider.GetRequiredService<IProfileService>().GetProfile(ProfileName);
            var storageFormatService = ServiceProvider.GetRequiredService<IStorageFormatService>();
            var storageFormat = storageFormatService.GetStorageFormats()
                .FirstOrDefault(testStorageFormat => testStorageFormat.CanDeserializeFrom(InputDirectory));

            if (storageFormat == null)
                throw new CommandException(
                    $"Cannot find storage format that is compatible with directory [{InputDirectory}].");

            var database = new Database(new DatabaseOptions(profile.GetGameId(), profile.GetDatabaseType()));
            _logger.LogInformation("Loading database from disk...");
            await storageFormat.DeserializeAsync(InputDirectory, database);
            _logger.LogInformation("Loaded database");

            var strList = new HashSet<string>();

            foreach (var vltClass in database.Classes)
            {
                if (!vltClass.Name.StartsWith("0x")) strList.Add(vltClass.Name);

                foreach (var vltClassField in vltClass.Fields.Values.Where(vltClassField =>
                    !vltClassField.Name.StartsWith("0x")))
                    strList.Add(vltClassField.Name);
            }

            foreach (var vltCollection in database.RowManager.EnumerateFlattenedCollections())
                if (!vltCollection.Name.StartsWith("0x"))
                    strList.Add(vltCollection.Name);

            await File.WriteAllLinesAsync(OutputPath, strList);
            _logger.LogInformation("Done!");
            return 0;
        }
    }
}