using System.IO;
using System.Threading.Tasks;
using Attribulator.API.Plugin;
using Attribulator.API.Services;
using CommandLine;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using VaultLib.Core.DB;

namespace Attribulator.CLI.Commands
{
    [Verb("unpack", HelpText = "Unpack binary VLT files.")]
    public class UnpackCommand : BaseCommand
    {
        [Option('i', HelpText = "Directory to read BIN files from", Required = true)]
        [UsedImplicitly]
        public string InputDirectory { get; set; }

        [Option('o', HelpText = "Directory to write unpacked files to", Required = true)]
        [UsedImplicitly]
        public string OutputDirectory { get; set; }

        [Option('p', HelpText = "The profile to use", Required = true)]
        [UsedImplicitly]
        public string ProfileName { get; set; }

        [Option('f', HelpText = "The format to use", Required = true)]
        [UsedImplicitly]
        public string StorageFormatName { get; set; }

        public override Task<int> Execute()
        {
            if (!Directory.Exists(InputDirectory))
                return Task.FromException<int>(
                    new DirectoryNotFoundException($"Cannot find input directory: {InputDirectory}"));

            if (!Directory.Exists(OutputDirectory)) Directory.CreateDirectory(OutputDirectory);

            var profile = ServiceProvider.GetRequiredService<IProfileService>().GetProfile(ProfileName);
            var storageFormat = ServiceProvider.GetRequiredService<IStorageFormatService>()
                .GetStorageFormat(StorageFormatName);
            var database = new Database(new DatabaseOptions(profile.GetGameId(), profile.GetDatabaseType()));
            var files = profile.LoadFiles(database, InputDirectory);
            database.CompleteLoad();

            storageFormat.Serialize(database, OutputDirectory, files);

            return Task.FromResult(0);
        }
    }
}