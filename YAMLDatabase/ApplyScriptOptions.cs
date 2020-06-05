using CommandLine;
using JetBrains.Annotations;

namespace YAMLDatabase
{
    [Verb("apply-script")]
    [UsedImplicitly]
    public class ApplyScriptOptions : BaseOptions
    {
        [Option('s', HelpText = "The path to the .nfsms file")]
        [UsedImplicitly]
        public string ModScriptPath { get; set; }

        [Option("backup", HelpText = "Whether a YML backup should be made before saving the new database")]
        [UsedImplicitly]
        public bool MakeBackup { get; set; } = true;

        [Option("pack", HelpText = "Whether new bin files should be generated after applying the script")]
        [UsedImplicitly]
        public bool GenerateBins { get; set; } = true;
    }
}