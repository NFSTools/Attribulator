using System;
using System.Threading.Tasks;
using CommandLine;
using YAMLDatabase.API.Plugin;

namespace YAMLDatabase.Plugins.CoreCommands
{
    [Verb("pack", HelpText = "Packs a database to BIN files.")]
    public class PackCommand : BaseCommand
    {
        public override Task<int> Execute()
        {
            throw new NotImplementedException();
        }
    }
}