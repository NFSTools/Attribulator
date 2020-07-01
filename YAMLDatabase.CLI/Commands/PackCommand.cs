using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Dasync.Collections;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VaultLib.Core.DB;
using YAMLDatabase.API.Data;
using YAMLDatabase.API.Exceptions;
using YAMLDatabase.API.Plugin;
using YAMLDatabase.API.Services;
using YAMLDatabase.CLI.Build;

namespace YAMLDatabase.CLI.Commands
{
    [Verb("pack", HelpText = "Packs a database to BIN files.")]
    public class PackCommand : BaseCommand
    {
        [Option('i', "input", HelpText = "Directory to read unpacked files from", Required = true)]
        [UsedImplicitly]
        public string InputDirectory { get; set; }

        [Option('o', "output", HelpText = "Directory to write BIN files to", Required = true)]
        [UsedImplicitly]
        public string OutputDirectory { get; set; }

        [Option('p', "profile", HelpText = "The profile to use", Required = true)]
        [UsedImplicitly]
        public string ProfileName { get; set; }

        [Option('c', "cache", HelpText = "Whether to use the build cache")]
        [UsedImplicitly]
        public bool UseCache { get; set; }

        public override async Task<int> Execute()
        {
            var logger = ServiceProvider.GetRequiredService<ILogger<PackCommand>>();

            if (!Directory.Exists(InputDirectory))
                throw new DirectoryNotFoundException($"Cannot find input directory: {InputDirectory}");

            if (!Directory.Exists(OutputDirectory)) Directory.CreateDirectory(OutputDirectory);

            var profile = ServiceProvider.GetRequiredService<IProfileService>().GetProfile(ProfileName);
            var storageFormatService = ServiceProvider.GetRequiredService<IStorageFormatService>();
            var storageFormat = storageFormatService.GetStorageFormats()
                .FirstOrDefault(testStorageFormat => testStorageFormat.CanDeserializeFrom(InputDirectory));

            if (storageFormat == null)
                throw new CommandException(
                    $"Cannot find storage format that is compatible with directory [{InputDirectory}].");

            var database = new Database(new DatabaseOptions(profile.GetGameId(), profile.GetDatabaseType()));
            logger.LogInformation("Loading database from disk...");
            var files = storageFormat.Deserialize(InputDirectory, database).ToList();
            logger.LogInformation("Loaded database");

            // Parallel hash check
            var filesToCompile = new ConcurrentBag<LoadedFile>();
            var cache = new BuildCache();
            var dbInternalPath = Path.Combine(InputDirectory, ".db");
            var cacheFilePath = Path.Combine(dbInternalPath, ".cache.json");

            if (UseCache)
            {
                if (Directory.Exists(dbInternalPath) && File.Exists(cacheFilePath))
                {
                    // Load cache from file
                    logger.LogInformation("Loading cache from disk...");
                    cache = JsonConvert.DeserializeObject<BuildCache>(await File.ReadAllTextAsync(cacheFilePath));
                    logger.LogInformation("Loaded cache from disk");
                }

                logger.LogInformation("Performing cache check...");

                await files.ParallelForEachAsync(async f =>
                {
                    var storageHash = await storageFormat.ComputeHashAsync(InputDirectory, f);
                    var cacheKey = $"{f.Group}_{f.Name}";
                    var cacheHash = cache.GetHash(cacheKey);

                    if (cacheHash != storageHash)
                    {
                        filesToCompile.Add(f);
                        cache.HashMap[cacheKey] = storageHash;
                        logger.LogInformation("Detected change in file {Group}[{Name}]", f.Group, f.Name);
                    }
                }, Environment.ProcessorCount);
            }
            else
            {
                filesToCompile = new ConcurrentBag<LoadedFile>(files);
            }

            logger.LogInformation("Saving files...");
            profile.SaveFiles(database, OutputDirectory, filesToCompile.ToList());

            if (UseCache)
            {
                logger.LogInformation("Writing cache...");
                Directory.CreateDirectory(dbInternalPath);
                await File.WriteAllTextAsync(cacheFilePath, JsonConvert.SerializeObject(cache));
            }

            logger.LogInformation("Done!");
            return 0;
        }
    }
}