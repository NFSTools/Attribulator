using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Attribulator.API.Plugin;
using Attribulator.API.Services;
using CommandLine;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VaultLib.Core.Data;
using VaultLib.Core.DB;

namespace Attribulator.CLI.Commands
{
    [Verb("dump", HelpText = "Dump binary VLT files to JSON.")]
    public class DumpCommand : BaseCommand
    {
        private ILogger<DumpCommand> _logger;

        [Option('i', HelpText = "Directory to read BIN files from", Required = true)]
        [UsedImplicitly]
        public string InputDirectory { get; set; }

        [Option('o', HelpText = "Directory to write unpacked files to", Required = true)]
        [UsedImplicitly]
        public string OutputDirectory { get; set; }

        [Option('p', HelpText = "The profile to use", Required = true)]
        [UsedImplicitly]
        public string ProfileName { get; set; }

        public override void SetServiceProvider(IServiceProvider serviceProvider)
        {
            base.SetServiceProvider(serviceProvider);

            _logger = ServiceProvider.GetRequiredService<ILogger<DumpCommand>>();
        }

        public override Task<int> Execute()
        {
            if (!Directory.Exists(InputDirectory))
                return Task.FromException<int>(
                    new DirectoryNotFoundException($"Cannot find input directory: {InputDirectory}"));

            if (!Directory.Exists(OutputDirectory)) Directory.CreateDirectory(OutputDirectory);

            var profile = ServiceProvider.GetRequiredService<IProfileService>().GetProfile(ProfileName);
            var database = new Database(new DatabaseOptions(profile.GetGameId(), profile.GetDatabaseType()));
            _logger.LogInformation("Loading database from disk...");
            profile.LoadFiles(database, InputDirectory);
            database.CompleteLoad();
            _logger.LogInformation("Unpacking database to disk...");

            foreach (var vltClass in database.Classes)
            {
                var dumpedClassData = new DumpedClassData
                    {Class = vltClass, Collections = new List<DumpedCollection>()};
                foreach (var vltCollection in database.RowManager.GetFlattenedCollections(vltClass.Name))
                    dumpedClassData.Collections.Add(new DumpedCollection
                    {
                        Name = vltCollection.Name,
                        ParentName = vltCollection.Parent?.Name,
                        Data = vltCollection.GetData()
                            .ToDictionary(e => e.Key, e => vltCollection.GetDataValue<object>(e.Key))
                    });

                File.WriteAllText(Path.Combine(OutputDirectory, $"{vltClass.Name}.json"),
                    JsonConvert.SerializeObject(dumpedClassData, Formatting.Indented));
            }

            _logger.LogInformation("Done!");
            return Task.FromResult(0);
        }

        private class DumpedCollection
        {
            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("parent_name")] public string ParentName { get; set; }
            [JsonProperty("data")] public Dictionary<string, object> Data { get; set; }
        }

        private class DumpedClassData
        {
            [JsonProperty("class")] public VltClass Class { get; set; }
            [JsonProperty("collections")] public List<DumpedCollection> Collections { get; set; }
        }
    }
}