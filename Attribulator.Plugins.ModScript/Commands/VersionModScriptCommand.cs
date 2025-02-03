using System.Collections.Generic;
using Attribulator.ModScript.API;

namespace Attribulator.Plugins.ModScript.Commands
{
    public class VersionModScriptCommand : BaseModScriptCommand, IParseableModScriptCommand<VersionModScriptCommand>
    {
        public required string Version { get; init; }

        static VersionModScriptCommand IParseableModScriptCommand<VersionModScriptCommand>.Parse(List<string> parts)
        {
            return new VersionModScriptCommand
            {
                Version = parts[1]
            };
        }

        protected override void Execute<TKey>(DatabaseHelper<TKey> databaseHelper)
        {
            if (Version != "4.6")
                throw new CommandExecutionException(
                    "This tool is only compatible with ModScript files for NFS-VltEd 4.6.");
        }
    }
}