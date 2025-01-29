#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Attribulator.API;
using Attribulator.API.Exceptions;
using Attribulator.API.Plugin;
using Attribulator.API.Services;
using Attribulator.API.Utils;
using CommandLine;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;
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

            var profile = FindProfile(ProfileName);

            switch (profile)
            {
                case IProfile<Key32> profile32:
                    ExecuteInternal(profile32);
                    break;
                case IProfile<Key64> profile64:
                    ExecuteInternal(profile64);
                    break;
                default:
                    throw new CommandException("Profile is not supported");
            }

            _logger.LogInformation("Done!");
            return Task.FromResult(0);
        }

        private void ExecuteInternal<TKey>(IProfile<TKey> profile) where TKey : struct, IKey<TKey>
        {
            var database = profile.CreateDatabase();
            _logger.LogInformation("Loading database from disk...");
            profile.LoadFiles(database, InputDirectory);
            database.CompleteLoad();
            _logger.LogInformation("Unpacking database to disk...");

            foreach (var vltClass in database.Classes)
            {
                var dumpedClassData = new DumpedClassData<TKey>
                    { Class = vltClass, Collections = new List<DumpedCollection<TKey>>() };
                foreach (var vltCollection in database.RowManager.GetCollections(vltClass.Key))
                {
                    dumpedClassData.Collections.Add(new DumpedCollection<TKey>
                    {
                        Name = KeyUtils.KeyToString(vltCollection.Key),
                        ParentName = vltCollection.Parent is { Key: var pk } ? KeyUtils.KeyToString(pk) : null,
                        Data = vltCollection.GetData()
                            .ToDictionary(e => KeyUtils.KeyToString(e.Key), e => e.Value)
                    });
                }

                File.WriteAllText(Path.Combine(OutputDirectory, $"{vltClass.Key}.json"),
                    JsonConvert.SerializeObject(dumpedClassData, Formatting.Indented));
            }
        }

        private class DumpedCollection<TKey> where TKey : struct, IKey<TKey>
        {
            [JsonProperty("name")] public required string Name { get; set; }
            [JsonProperty("parent_name")] public string? ParentName { get; set; }
            [JsonProperty("data")] public Dictionary<string, object> Data { get; set; }
        }

        private class DumpedClassData<TKey> where TKey : struct, IKey<TKey>
        {
            [JsonProperty("class")] public VltClass<TKey> Class { get; set; }
            [JsonProperty("collections")] public List<DumpedCollection<TKey>> Collections { get; set; }
        }
    }
}