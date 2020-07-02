using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VaultLib.Core.DB;
using YAMLDatabase.API.Exceptions;
using YAMLDatabase.API.Plugin;
using YAMLDatabase.API.Services;

namespace YAMLDatabase.Plugins.ModScript
{
    [Verb("apply-script", HelpText = "Apply a ModScript to a database.")]
    [UsedImplicitly]
    public class ApplyScriptCommand : BaseCommand
    {
        private ILogger<ApplyScriptCommand> _logger;

        [Option('i', HelpText = "Directory to read unpacked files from", Required = true)]
        [UsedImplicitly]
        public string InputDirectory { get; set; }

        [Option('o', HelpText = "Directory to write BIN files to", Required = true)]
        [UsedImplicitly]
        public string OutputDirectory { get; set; }

        [Option('p', HelpText = "The ID of the profile to use", Required = true)]
        [UsedImplicitly]
        public string ProfileName { get; set; }

        [Option('s', HelpText = "The path to the .nfsms file", Required = true)]
        [UsedImplicitly]
        public string ModScriptPath { get; set; }

        [Option("no-backup", HelpText = "Disable backup generation.")]
        [UsedImplicitly]
        public bool DisableBackup { get; set; }

        [Option("no-bins", HelpText = "Disable binary generation.")]
        [UsedImplicitly]
        public bool DisableBinGeneration { get; set; }

        public override void SetServiceProvider(IServiceProvider serviceProvider)
        {
            base.SetServiceProvider(serviceProvider);

            _logger = ServiceProvider.GetRequiredService<ILogger<ApplyScriptCommand>>();
        }

        public override async Task<int> Execute()
        {
            if (!Directory.Exists(InputDirectory))
                throw new DirectoryNotFoundException($"Cannot find input directory: {InputDirectory}");

            if (!Directory.Exists(OutputDirectory)) Directory.CreateDirectory(OutputDirectory);

            if (!File.Exists(ModScriptPath))
                throw new FileNotFoundException($"Cannot find ModScript file: {ModScriptPath}");

            var profile = ServiceProvider.GetRequiredService<IProfileService>().GetProfile(ProfileName);
            var storageFormatService = ServiceProvider.GetRequiredService<IStorageFormatService>();
            var storageFormat = storageFormatService.GetStorageFormats()
                .FirstOrDefault(testStorageFormat => testStorageFormat.CanDeserializeFrom(InputDirectory));

            if (storageFormat == null)
                throw new CommandException(
                    $"Cannot find storage format that is compatible with directory [{InputDirectory}].");

            var database = new Database(new DatabaseOptions(profile.GetGameId(), profile.GetDatabaseType()));
            _logger.LogInformation("Loading database from disk...");
            var files = (await storageFormat.DeserializeAsync(InputDirectory, database)).ToList();
            _logger.LogInformation("Loaded database");

            var modScriptParser = new ModScriptParser(ModScriptPath);
            var modScriptDatabase = new ModScriptDatabaseHelper(database);
            var scriptStopwatch = Stopwatch.StartNew();
            var numCommands = 0L;

            foreach (var command in modScriptParser.Parse())
                try
                {
                    command.Execute(modScriptDatabase);
                    numCommands++;
                }
                catch (Exception e)
                {
                    throw new ModScriptCommandExecutionException($"Failed to execute command: {command.Line}", e);
                }

            scriptStopwatch.Stop();

            var commandsPerSecond = (ulong) (numCommands / (scriptStopwatch.ElapsedMilliseconds / 1000.0));
            _logger.LogInformation(
                "Applied {NumCommands} command(s) from script in {ElapsedMilliseconds}ms ({Duration}; ~ {NumPerSec}/sec)",
                numCommands, scriptStopwatch.ElapsedMilliseconds, scriptStopwatch.Elapsed, commandsPerSecond);

            if (!DisableBackup)
            {
                _logger.LogInformation("Generating backup");
                Directory.Move(InputDirectory,
                    $"{InputDirectory.TrimEnd('/', '\\')}_{DateTimeOffset.Now.ToUnixTimeSeconds()}");
                Directory.CreateDirectory(InputDirectory);
            }

            _logger.LogInformation("Saving database");
            storageFormat.Serialize(database, InputDirectory, files);

            // TODO: should build cache be updated?

            if (!DisableBinGeneration)
            {
                _logger.LogInformation("Saving binaries");
                profile.SaveFiles(database, OutputDirectory, files);
            }

            _logger.LogInformation("Done!");

            return 0;
        }
    }
}