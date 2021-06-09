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
using VaultLib.Core;
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

        [Option("dry-run",
            HelpText =
                "Perform a \"dry run\", which will attempt to execute every script command, record errors, and not save new files.")]
        [UsedImplicitly]
        public bool DryRun { get; set; }

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

            var scriptFiles = new List<string>();

            foreach (var scriptFile in ModScriptPaths)
                if (!File.Exists(scriptFile))
                {
                    if (!Directory.Exists(scriptFile))
                        throw new FileNotFoundException($"Cannot find ModScript file or folder: {scriptFile}");

                    scriptFiles.AddRange(Directory.GetFiles(scriptFile, "*.nfsms"));
                }
                else
                {
                    scriptFiles.Add(scriptFile);
                }

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

            var modScriptDatabase = new DatabaseHelper(database);
            var totalCommands = 0L;
            var totalMilliseconds = 0.0d;
            var errorsDict = new Dictionary<string, List<(long, string, Exception)>>();

            foreach (var scriptFile in scriptFiles)
            {
                _logger.LogInformation("Processing script: {FileName}", scriptFile);
                var fileStopwatch = Stopwatch.StartNew();
                var numCommands = 0L;
                var errors = new List<(long, string, Exception)>();

                foreach (var command in _modScriptService.ParseCommands(File.ReadLines(scriptFile)))
                    try
                    {
                        command.Execute(modScriptDatabase);
                        numCommands++;
                    }
                    catch (Exception e)
                    {
                        if (DryRun)
                        {
                            errors.Add((command.LineNumber, command.Line, e));
                        }
                        else
                        {
                            _logger.LogError(e, "Failed to execute script command at line {LineNumber}: {Line}",
                                command.LineNumber, command.Line);
                            return 1;
                        }
                    }

                fileStopwatch.Stop();

                var commandsPerSecond = (ulong) (numCommands / (fileStopwatch.Elapsed.TotalMilliseconds / 1000.0));
                _logger.LogInformation(
                    "Applied {NumCommands} command(s){ErrorsDescription} from script [{FileName}] in {ElapsedMilliseconds}ms ({Duration}; ~ {NumPerSec}/sec)",
                    numCommands,
                    GetErrorsBrief(errors.Count),
                    scriptFile, fileStopwatch.ElapsedMilliseconds, fileStopwatch.Elapsed,
                    commandsPerSecond);

                totalCommands += numCommands;
                totalMilliseconds += fileStopwatch.Elapsed.TotalMilliseconds;
                errorsDict.Add(scriptFile, errors);
            }

            var totalCommandsPerSecond =
                (ulong) (totalCommands / (totalMilliseconds / 1000.0));

            _logger.LogInformation(
                "Overall: Applied {NumCommands} command(s){ErrorsDescription} from {NumScripts} script(s) (execution time: {ElapsedMilliseconds}ms / {Duration}; ~ {NumPerSec}/sec)",
                totalCommands, GetErrorsBrief(errorsDict.Sum(e => e.Value.Count)), scriptFiles.Count,
                Math.Round(totalMilliseconds),
                TimeSpan.FromMilliseconds(totalMilliseconds),
                totalCommandsPerSecond);

            if (DryRun)
            {
                foreach (var scriptFile in scriptFiles)
                {
                    var errors = errorsDict[scriptFile];

                    _logger.LogError("{FileName}: {ErrorCount} error(s)", scriptFile, errors.Count);
                    foreach (var (lineNumber, lineContents, exception) in errors)
                        _logger.LogError(exception, "\tFailed to execute script command at line {LineNumber}: {Line}",
                            lineNumber, lineContents);
                }
            }
            else
            {
                var modifiedVaultNames = modScriptDatabase.GetModifiedVaults().ToList();

                if (modifiedVaultNames.Count > 0)
                {
                    _logger.LogInformation("Saving database");

                    bool VaultFilter(Vault vault)
                    {
                        return modifiedVaultNames.Contains(vault.Name);
                    }

                    var modifiedFiles = files.Where(f => f.Vaults.Any(VaultFilter)).ToList();

                    if (!DisableBackup)
                    {
                        var backupDir = Path.Combine(InputDirectory,
                            $"backup_{DateTimeOffset.Now.ToUnixTimeSeconds()}");
                        Directory.CreateDirectory(backupDir);
                        foreach (var modifiedFile in modifiedFiles)
                            storageFormat.Backup(InputDirectory, backupDir, modifiedFile,
                                modifiedFile.Vaults.Where(v => modifiedVaultNames.Contains(v.Name)));
                    }

                    storageFormat.Serialize(database, InputDirectory, files, VaultFilter);

                    // TODO: should build cache be updated?

                    if (!DisableBinGeneration)
                    {
                        _logger.LogInformation("Saving binaries");
                        profile.SaveFiles(database, OutputDirectory, modifiedFiles);
                    }
                }
                else
                {
                    // TODO: Currently this can't happen unless the script is empty. We need actual change detection.
                    _logger.LogInformation("No changes detected");
                }
            }

            _logger.LogInformation("Done!");

            return 0;
        }

        private static string GetErrorsBrief(long num)
        {
            return num switch
            {
                0 => string.Empty,
                1 => " (with 1 error)",
                _ => $" (with {num} errors)"
            };
        }
    }
}