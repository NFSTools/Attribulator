using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Attribulator.API;
using Attribulator.API.Exceptions;
using Attribulator.API.Plugin;
using Attribulator.API.Serialization;
using Attribulator.API.Services;
using Attribulator.API.Utils;
using CommandLine;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VaultLib.Core.DataInterfaces;
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

            var strList = new HashSet<string>();

            switch (profile)
            {
                case IProfile<Key32> profile32:
                    await GenerateHashListAsync(profile32, storageFormat, strList);
                    break;
                case IProfile<Key64> profile64:
                    await GenerateHashListAsync(profile64, storageFormat, strList);
                    break;
                default:
                    throw new CommandException("Profile is not supported");
            }

            await File.WriteAllLinesAsync(OutputPath, strList);
            _logger.LogInformation("Exported {NumEntries} entries to {OutPath}", strList.Count, OutputPath);
            return 0;
        }

        private async Task GenerateHashListAsync<TKey>(IProfile<TKey> profile, IDatabaseStorageFormat storageFormat,
            HashSet<string> strList) where TKey : struct, IKey<TKey>
        {
            var database = profile.CreateDatabase();
            _logger.LogInformation("Loading database from disk...");
            await storageFormat.DeserializeAsync(InputDirectory, database);
            _logger.LogInformation("Loaded database");


            foreach (var vltClass in database.Classes)
            {
                if (KeyUtils.KeyToOptString(vltClass.Key) is { } className)
                {
                    strList.Add(className);
                }

                foreach (var field in vltClass.Fields.Values)
                {
                    if (KeyUtils.KeyToOptString(field.Key) is { } fieldName)
                    {
                        strList.Add(fieldName);
                    }
                }
            }

            foreach (var vltCollection in database.RowManager.EnumerateCollections())
            {
                if (KeyUtils.KeyToOptString(vltCollection.Key) is { } collectionName)
                {
                    strList.Add(collectionName);
                }
            }
        }
    }
}