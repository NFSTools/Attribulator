using CommandLine;
using JetBrains.Annotations;
using YAMLDatabase.API.Plugin;

namespace YAMLDatabase.Plugins.ModScript
{
    [Verb("apply-script", HelpText = "Applies a ModScript to a YML database.")]
    [UsedImplicitly]
    public class ApplyScriptCommand : ICommand
    {
        [Option('i', HelpText = "Directory to read YML files from", Required = true)]
        [UsedImplicitly]
        public string InputDirectory { get; set; }

        [Option('o', HelpText = "Directory to write BIN files to", Required = true)]
        [UsedImplicitly]
        public string OutputDirectory { get; set; }
        
        [Option('p', HelpText = "The ID of the profile to use", Required = true)]
        [UsedImplicitly]
        public string ProfileName { get; set; }
        
        public int Execute()
        {
            throw new System.NotImplementedException();
        }
    }
}