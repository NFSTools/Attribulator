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
            HelpText = "One or more hash values, in either hexadecimal or decimal format.")]
        public IEnumerable<string> HashValues { get; [UsedImplicitly] set; }

        [Option("dictionary", Required = false, HelpText = "The path to an additional hash dictionary to load.")]
        public string DictionaryPath { get; [UsedImplicitly] set; }

        public override void SetServiceProvider(IServiceProvider serviceProvider)
        {
            base.SetServiceProvider(serviceProvider);

            _logger = ServiceProvider.GetRequiredService<ILogger<ResolveHashesCommand>>();
        }

        public override Task<int> Execute()
        {
            if (DictionaryPath != null)
            {
                HashManager.LoadDictionary(DictionaryPath);
            }

            foreach (var hashValue in HashValues)
            {
                ulong parsedHash;

                if (hashValue.StartsWith("0x"))
                {
                    if (!ulong.TryParse(hashValue.AsSpan(2), NumberStyles.AllowHexSpecifier,
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

                string resolved32 = null, resolved64 = HashManager.ResolveVlt(parsedHash);

                if (parsedHash <= uint.MaxValue)
                {
                    resolved32 = HashManager.ResolveVlt((uint)parsedHash);
                }

                if (resolved32 == null && resolved64 == null)
                {
                    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                    if (parsedHash <= uint.MaxValue)
                    {
                        _logger.LogWarning("No result for 0x{HashValue:X8}", parsedHash);
                    }
                    else
                    {
                        _logger.LogWarning("No result for 0x{HashValue:X16}", parsedHash);
                    }
                }
                else
                {
                    if (resolved32 != null)
                    {
                        _logger.LogInformation("Hash32 0x{HashValue:X8} -> {ResolvedValue}", parsedHash, resolved32);
                    }

                    if (resolved64 != null)
                    {
                        _logger.LogInformation("Hash64 0x{HashValue:X16} -> {ResolvedValue}", parsedHash, resolved64);
                    }
                }
            }

            return Task.FromResult(0);
        }
    }
}