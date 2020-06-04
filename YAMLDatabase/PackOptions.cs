using System.Collections.Generic;
using CommandLine;

namespace YAMLDatabase
{
    [Verb("pack")]
    public class PackOptions : BaseOptions
    {
        [Option('v', "files", HelpText = "The list of files to pack (if omitted, all files are packed)")]
        public IEnumerable<string> Files { get; set; }
    }
}