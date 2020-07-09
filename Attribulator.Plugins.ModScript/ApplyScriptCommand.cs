using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Attribulator.API.Exceptions;
using Attribulator.API.Plugin;
using Attribulator.API.Services;
using Attribulator.ModScript.API;
using CommandLine;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VaultLib.Core.DB;

namespace Attribulator.Plugins.ModScript
{
    [Verb("apply-script", HelpText = "Apply a ModScript to a database.")]
    [UsedImplicitly]
    public class ApplyScriptCommand : BaseCommand
    {
        private ILogger<ApplyScriptCommand> _logger;
        private IModScriptService _modScriptService;

        [Option('i', HelpText = "Directory to read unpacked files from", Required = true)]
        [UsedImplicitly]
        public string InputDirectory { get; set; }

        [Option('o', HelpText = "Directory to write BIN files to", Required = true)]
        [UsedImplicitly]
        public string OutputDirectory { get; set; }

        [Option('p', HelpText = "The ID of the profile to use", Required = true)]
        [UsedImplicitly]
        public string ProfileName { get; set; }

        [Option('s', HelpText = "The path(s) to the .nfsms file(s)", Required = true)]
        [UsedImplicitly]
        public IEnumerable<string> ModScriptPaths { get; set; }

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
            _modScriptService = ServiceProvider.GetRequiredService<IModScriptService>();
        }

        public override async Task<int> Execute()
        {
            if (!Directory.Exists(InputDirectory))
                throw new DirectoryNotFoundException($"Cannot find input directory: {InputDirectory}");

            var scriptFiles = ModScriptPaths.ToList();

            foreach (var scriptFile in scriptFiles)
                if (!File.Exists(scriptFile))
                    throw new FileNotFoundException($"Cannot find ModScript file: {scriptFile}");

            if (!Directory.Exists(OutputDirectory)) Directory.CreateDirectory(OutputDirectory);

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

            var overallStopwatch = Stopwatch.StartNew();
            var modScriptDatabase = new DatabaseHelper(database);
            var totalCommands = 0L;
            var totalMilliseconds = 0.0d;

            foreach (var scriptFile in scriptFiles)
            {
                _logger.LogInformation("Processing script: {FileName}", scriptFile);
                var fileStopwatch = Stopwatch.StartNew();
                var numCommands = 0L;

                foreach (var command in _modScriptService.ParseCommands(File.ReadLines(scriptFile)))
                    try
                    {
                        command.Execute(modScriptDatabase);
                        numCommands++;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Failed to execute script command at line {LineNumber}: {Line}",
                            command.LineNumber, command.Line);
                        return 1;
                    }

                fileStopwatch.Stop();

                var commandsPerSecond = (ulong) (numCommands / (fileStopwatch.Elapsed.TotalMilliseconds / 1000.0));
                _logger.LogInformation(
                    "Applied {NumCommands} command(s) from script [{FileName}] in {ElapsedMilliseconds}ms ({Duration}; ~ {NumPerSec}/sec)",
                    numCommands, scriptFile, fileStopwatch.ElapsedMilliseconds, fileStopwatch.Elapsed,
                    commandsPerSecond);

                totalCommands += numCommands;
                totalMilliseconds += fileStopwatch.Elapsed.TotalMilliseconds;
            }

            overallStopwatch.Stop();
            var totalCommandsPerSecond =
                (ulong) (totalCommands / (totalMilliseconds / 1000.0));

            _logger.LogInformation(
                "Completed in {OverallTimeInMilliseconds}ms ({OverallDuration}).", overallStopwatch.ElapsedMilliseconds,
                overallStopwatch.Elapsed);

            _logger.LogInformation(
                "Overall: Applied {NumCommands} command(s) from {NumScripts} script(s) (execution time: {ElapsedMilliseconds}ms / {Duration}; ~ {NumPerSec}/sec)",
                totalCommands, scriptFiles.Count, Math.Round(totalMilliseconds),
                TimeSpan.FromMilliseconds(totalMilliseconds),
                totalCommandsPerSecond);

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