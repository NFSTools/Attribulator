using System.Threading.Tasks;
using CommandLine;
using YAMLDatabase.API.Plugin;

namespace YAMLDatabase.Plugins.CoreCommands
{
    [Verb("unpack", HelpText = "Unpacks binary VLT files to YML files.")]
    public class UnpackCommand : BaseCommand
    {
        public override Task<int> Execute()
        {
            throw new System.NotImplementedException();
        }
    }
}