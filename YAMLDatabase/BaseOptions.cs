using CommandLine;
using JetBrains.Annotations;

namespace YAMLDatabase
{
    public abstract class BaseOptions
    {
        [Option('i', HelpText = "Directory to read files (.yml or .bin) from", Required = true)]
        [UsedImplicitly]
        public string InputDirectory { get; set; }

        [Option('o', HelpText = "Directory to write files (.yml or .bin) to", Required = true)]
        [UsedImplicitly]
        public string OutputDirectory { get; set; }
        
        [Option('p', HelpText = "The profile to use", Required = true)]
        [UsedImplicitly]
        public string ProfileName { get; set; }
    }
}