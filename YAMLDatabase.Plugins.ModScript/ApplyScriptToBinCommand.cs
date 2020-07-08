using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VaultLib.Core.DB;
using YAMLDatabase.API.Plugin;
using YAMLDatabase.API.Services;
using YAMLDatabase.ModScript.API;

namespace YAMLDatabase.Plugins.ModScript
{
    [Verb("apply-script-bin", HelpText = "Apply a ModScript to a compiled database.")]
    [UsedImplicitly]
    public class ApplyScriptToBinCommand : BaseCommand
    {
        private ILogger<ApplyScriptToBinCommand> _logger;
        private IModScriptService _modScriptService;

        [Option('i', HelpText = "Directory to read compiled files from", Required = true)]
        [UsedImplicitly]
        public string InputDirectory { get; set; }

        [Option('o', HelpText = "Directory to write new files to", Required = true)]
        [UsedImplicitly]
        public string OutputDirectory { get; set; }

        [Option('p', HelpText = "The ID of the profile to use", Required = true)]
        [UsedImplicitly]
        public string ProfileName { get; set; }

        [Option('s', HelpText = "The path to the .nfsms file", Required = true)]
        [UsedImplicitly]
        public string ModScriptPath { get; set; }

        public override void SetServiceProvider(IServiceProvider serviceProvider)
        {
            base.SetServiceProvider(serviceProvider);

            _logger = ServiceProvider.GetRequiredService<ILogger<ApplyScriptToBinCommand>>();
            _modScriptService = ServiceProvider.GetRequiredService<IModScriptService>();
        }

        public override Task<int> Execute()
        {
            if (!Directory.Exists(InputDirectory))
                return Task.FromException<int>(
                    new DirectoryNotFoundException($"Cannot find input directory: {InputDirectory}"));

            if (!File.Exists(ModScriptPath))
                throw new FileNotFoundException($"Cannot find ModScript file: {ModScriptPath}");

            if (!Directory.Exists(OutputDirectory)) Directory.CreateDirectory(OutputDirectory);

            var profile = ServiceProvider.GetRequiredService<IProfileService>().GetProfile(ProfileName);
            _logger.LogInformation("Loading database from disk...");
            var database = new Database(new DatabaseOptions(profile.GetGameId(), profile.GetDatabaseType()));
            var files = profile.LoadFiles(database, InputDirectory);
            database.CompleteLoad();
            _logger.LogInformation("Loaded database");

            var modScriptDatabase = new DatabaseHelper(database);
            var scriptStopwatch = Stopwatch.StartNew();
            var numCommands = 0L;

            foreach (var command in _modScriptService.ParseCommands(File.ReadLines(ModScriptPath)))
                try
                {
                    command.Execute(modScriptDatabase);
                    numCommands++;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to execute script command at line {LineNumber}: {Line}",
                        command.LineNumber, command.Line);
                    return Task.FromResult(1);
                }

            scriptStopwatch.Stop();

            var commandsPerSecond = (ulong) (numCommands / (scriptStopwatch.ElapsedMilliseconds / 1000.0));
            _logger.LogInformation(
                "Applied {NumCommands} command(s) from script in {ElapsedMilliseconds}ms ({Duration}; ~ {NumPerSec}/sec)",
                numCommands, scriptStopwatch.ElapsedMilliseconds, scriptStopwatch.Elapsed, commandsPerSecond);

            _logger.LogInformation("Saving binaries");
            profile.SaveFiles(database, OutputDirectory, files);

            _logger.LogInformation("Done!");

            return Task.FromResult(0);
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            var dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);

            var dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName)) Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
                foreach (var subdir in dirs)
                {
                    var temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
        }
    }
}