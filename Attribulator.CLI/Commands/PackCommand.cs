using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Attribulator.API.Data;
using Attribulator.API.Exceptions;
using Attribulator.API.Plugin;
using Attribulator.API.Services;
using Attribulator.CLI.Build;
using CommandLine;
using Dasync.Collections;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VaultLib.Core.DB;

namespace Attribulator.CLI.Commands
{
    [Verb("pack", HelpText = "Pack a database to BIN files.")]
    public class PackCommand : BaseCommand
    {
        private ILogger<PackCommand> _logger;

        [Option('i', "input", HelpText = "Directory to read unpacked files from", Required = true)]
        [UsedImplicitly]
        public string InputDirectory { get; set; }

        [Option('o', "output", HelpText = "Directory to write BIN files to", Required = true)]
        [UsedImplicitly]
        public string OutputDirectory { get; set; }

        [Option('p', "profile", HelpText = "The profile to use", Required = true)]
        [UsedImplicitly]
        public string ProfileName { get; set; }

        [Option('c', "cache", HelpText = "Enable the build cache")]
        [UsedImplicitly]
        public bool UseCache { get; set; }

        [Option('b', "backup", HelpText = "Make a backup of BIN files")]
        [UsedImplicitly]
        public bool MakeBackup { get; set; }

        public override void SetServiceProvider(IServiceProvider serviceProvider)
        {
            base.SetServiceProvider(serviceProvider);

            _logger = ServiceProvider.GetRequiredService<ILogger<PackCommand>>();
        }

        public override async Task<int> Execute()
        {
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


            // Parallel hash check
            // TODO refactor build cache system to be reusable
            var fileNamesToCompile = new ConcurrentBag<string>();
            var cache = new BuildCache();
            var dbInternalPath = Path.Combine(InputDirectory, ".db");
            var cacheFilePath = Path.Combine(dbInternalPath, ".cache.json");

            var dbInfo = storageFormat.LoadInfo(InputDirectory);

            if (UseCache)
            {
                if (Directory.Exists(dbInternalPath) && File.Exists(cacheFilePath))
                {
                    // Load cache from file
                    _logger.LogInformation("Loading cache from disk...");
                    cache = JsonConvert.DeserializeObject<BuildCache>(await File.ReadAllTextAsync(cacheFilePath));
                    _logger.LogInformation("Loaded cache from disk");
                }

                _logger.LogInformation("Performing cache check...");

                var depMap = new Dictionary<string, List<string>>();

                foreach (var (key, value) in cache.Entries)
                foreach (var dependency in value.Dependencies)
                {
                    if (!depMap.ContainsKey(dependency)) depMap[dependency] = new List<string>();

                    depMap[dependency].Add(key);
                }

                await dbInfo.Files.ParallelForEachAsync(async f =>
                {
                    var storageHash = await storageFormat.ComputeHashAsync(InputDirectory, f);
                    var cacheKey = $"{f.Group}_{f.Name}";
                    var cacheEntry = cache.FindEntry(cacheKey) ?? new BuildCacheEntry();

                    if (cacheEntry.Hash != storageHash)
                    {
                        if (depMap.TryGetValue(cacheKey, out var depList))
                            foreach (var dep in depList)
                                fileNamesToCompile.Add(dep);
                        foreach (var dependency in cacheEntry.Dependencies) fileNamesToCompile.Add(dependency);
                        fileNamesToCompile.Add(f.Name);
                        cacheEntry.Hash = storageHash;
                        cacheEntry.LastModified = DateTimeOffset.Now;
                        cache.Entries[cacheKey] = cacheEntry;
                        _logger.LogInformation("Detected change in file {Group}[{Name}]", f.Group, f.Name);
                    }
                }, Environment.ProcessorCount);
            }
            else
            {
                // filesToCompile = new ConcurrentBag<LoadedFile>(files);
                fileNamesToCompile = new ConcurrentBag<string>(dbInfo.Files.Select(f => f.Name));
            }

            if (fileNamesToCompile.Count > 0)
            {
                var database = new Database(new DatabaseOptions(profile.GetGameId(), profile.GetDatabaseType()));
                _logger.LogInformation("Loading database from disk...");
                var files =
                    (await storageFormat.DeserializeAsync(InputDirectory, database, fileNamesToCompile)).ToList();
                _logger.LogInformation("Loaded database");
                _logger.LogInformation("Saving files...");
                var filesToCompile = files.Where(loadedFile => fileNamesToCompile.Contains(loadedFile.Name)).ToList();

                if (MakeBackup)
                {
                    _logger.LogInformation("Generating backup");
                    Directory.Move(OutputDirectory,
                        $"{OutputDirectory.TrimEnd('/', '\\')}_{DateTimeOffset.Now.ToUnixTimeSeconds()}");
                    Directory.CreateDirectory(OutputDirectory);
                }

                profile.SaveFiles(database, OutputDirectory, filesToCompile);

                if (UseCache)
                {
                    _logger.LogInformation("Writing cache...");
                    Directory.CreateDirectory(dbInternalPath);

                    var vaultFileMap = new Dictionary<string, string>();

                    foreach (var file in files)
                    foreach (var vault in file.Vaults)
                        vaultFileMap[vault.Name] = file.Name;

                    foreach (var f in filesToCompile)
                    {
                        var cacheKey = $"{f.Group}_{f.Name}";
                        var cacheEntry = cache.FindEntry(cacheKey);
                        cacheEntry.Dependencies = ComputeDependencies(vaultFileMap, f, database);
                    }

                    cache.LastUpdated = DateTimeOffset.Now;
                    await File.WriteAllTextAsync(cacheFilePath, JsonConvert.SerializeObject(cache));
                }
            }
            else
            {
                _logger.LogInformation("Binaries are up-to-date.");
            }

            _logger.LogInformation("Done!");
            return 0;
        }

        private static HashSet<string> ComputeDependencies(IReadOnlyDictionary<string, string> vaultFileMap,
            LoadedFile file,
            Database database)
        {
            var fileDependencies = new HashSet<string>();
            foreach (var vault in file.Vaults)
            {
                var collections = database.RowManager.GetCollectionsInVault(vault);

                foreach (var collection in collections)
                {
                    if (collection.Parent == null || collection.Parent.Vault == vault) continue;

                    var vaultFile = vaultFileMap[collection.Parent.Vault.Name];
                    if (vaultFile != file.Name) fileDependencies.Add(vaultFile);
                }
            }

            return fileDependencies;
        }
    }
}