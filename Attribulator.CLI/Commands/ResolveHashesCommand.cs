using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Attribulator.API.Plugin;
using CommandLine;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VaultLib.Core.Hashing;

namespace Attribulator.CLI.Commands
{
    [Verb("resolve-hashes", HelpText = "Attempt to resolve one or more hashes to their original text.")]
    public class ResolveHashesCommand : BaseCommand
    {
        private ILogger<ResolveHashesCommand> _logger;

        [Value(0, MetaName = "hashes", Required = true,
            HelpText = "One or more hash values, either in hexadecimal or decimal format.")]
        public IEnumerable<string> HashValues { get; [UsedImplicitly] set; }

        public override void SetServiceProvider(IServiceProvider serviceProvider)
        {
            base.SetServiceProvider(serviceProvider);

            _logger = ServiceProvider.GetRequiredService<ILogger<ResolveHashesCommand>>();
        }

        public override Task<int> Execute()
        {
            foreach (var hashValue in HashValues)
            {
                ulong parsedHash;

                if (hashValue.StartsWith("0x"))
                {
                    if (!ulong.TryParse(hashValue.Substring(2), NumberStyles.AllowHexSpecifier,
                        CultureInfo.InvariantCulture, out parsedHash))
                    {
                        _logger.LogError("Could not parse hash value as hexadecimal: {HashValue}", hashValue);
                        return Task.FromResult(1);
                    }
                }
                else
                {
                    if (!ulong.TryParse(hashValue, out parsedHash))
                    {
                        _logger.LogError("Could not parse hash value as decimal: {HashValue}", hashValue);
                        return Task.FromResult(1);
                    }
                }

                if (parsedHash <= uint.MaxValue)
                    _logger.LogInformation("Hash32 {HashValue:X8} -> {ResolvedValue}", parsedHash,
                        HashManager.ResolveVLT((uint) parsedHash));
                else
                    _logger.LogInformation("Hash64 {HashValue:X16} -> {ResolvedValue}", parsedHash,
                        HashManager.ResolveVLT(parsedHash));
            }

            return Task.FromResult(0);
        }
    }
}