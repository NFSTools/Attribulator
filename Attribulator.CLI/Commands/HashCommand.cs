using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Attribulator.API.Plugin;
using CommandLine;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VaultLib.Core.Hashing;

namespace Attribulator.CLI.Commands
{
    [Verb("hash", HelpText = "Attempt to hash one or more strings.")]
    public class HashCommand : BaseCommand
    {
        private ILogger<HashCommand> _logger;

        [Value(0, MetaName = "strings", Required = true,
            HelpText = "One or more strings to hash.")]
        public IEnumerable<string> Strings { get; [UsedImplicitly] set; }

        [Option("hash64", Required = false, HelpText = "Generate 64-bit hashes instead of 32-bit hashes")]
        public bool GenerateHash64 { get; [UsedImplicitly] set; }

        public override void SetServiceProvider(IServiceProvider serviceProvider)
        {
            base.SetServiceProvider(serviceProvider);

            _logger = ServiceProvider.GetRequiredService<ILogger<HashCommand>>();
        }

        public override Task<int> Execute()
        {
            foreach (var stringToHash in Strings)
                if (GenerateHash64)
                    _logger.LogInformation("{HashInput} -> 0x{HashOutput:X16}", stringToHash,
                        VLT64Hasher.Hash(stringToHash));
                else
                    _logger.LogInformation("{HashInput} -> 0x{HashOutput:X8}", stringToHash,
                        VLT32Hasher.Hash(stringToHash));

            return Task.FromResult(0);
        }
    }
}