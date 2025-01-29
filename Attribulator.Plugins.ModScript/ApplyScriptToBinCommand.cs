using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Attribulator.API;
using Attribulator.API.Exceptions;
using Attribulator.API.Plugin;
using Attribulator.API.Services;
using Attribulator.ModScript.API;
using CommandLine;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.DB;

namespace Attribulator.Plugins.ModScript
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

            return profile switch
            {
                IProfile<Key32> profile32 => ExecuteInternal(profile32),
                IProfile<Key64> profile64 => ExecuteInternal(profile64),
                _ => throw new CommandException("Profile is not supported")
            };
        }

        private Task<int> ExecuteInternal<TKey>(IProfile<TKey> profile) where TKey : struct, IKey<TKey>
        {
            var database = profile.CreateDatabase();
            var files = profile.LoadFiles(database, InputDirectory);
            database.CompleteLoad();
            _logger.LogInformation("Loaded database");

            var modScriptDatabase = new DatabaseHelper<TKey>(database);
            var scriptStopwatch = Stopwatch.StartNew();
            var numCommands = 0L;

            foreach (var command in _modScriptService.ParseCommands(File.ReadLines(ModScriptPath)))
            {
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
            }

            scriptStopwatch.Stop();

            var commandsPerSecond = (ulong)(numCommands / (scriptStopwatch.ElapsedMilliseconds / 1000.0));
            _logger.LogInformation(
                "Applied {NumCommands} command(s) from script in {ElapsedMilliseconds}ms ({Duration}; ~ {NumPerSec}/sec)",
                numCommands, scriptStopwatch.ElapsedMilliseconds, scriptStopwatch.Elapsed, commandsPerSecond);

            _logger.LogInformation("Saving binaries");
            profile.SaveFiles(database, OutputDirectory, files);

            _logger.LogInformation("Done!");

            return Task.FromResult(0);
        }
    }
}