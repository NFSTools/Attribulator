using CommandLine;

namespace YAMLDatabase
{
    [Verb("apply-script")]
    public class ApplyScriptOptions : BaseOptions
    {
        [Option('s', HelpText = "The path to the .nfsms file")]
        public string ModScriptPath { get; set; }
    }
}