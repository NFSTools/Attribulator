using CommandLine;

namespace YAMLDatabase
{
    [Verb("apply-script")]
    public class ApplyScriptOptions : BaseOptions
    {
        [Option('s', HelpText = "The path to the .nfsms file")]
        public string ModScriptPath { get; set; }

        [Option("backup", HelpText = "Whether a YML backup should be made before saving the new database")]
        public bool MakeBackup { get; set; } = true;

        [Option("pack", HelpText = "Whether new bin files should be generated after applying the script")]
        public bool GenerateBins { get; set; } = true;
    }
}