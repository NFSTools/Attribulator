using System.Collections.Generic;
using CommandLine;
using JetBrains.Annotations;

namespace YAMLDatabase
{
    [Verb("pack")]
    [UsedImplicitly]
    public class PackOptions : BaseOptions
    {
        [Option('f', "files", HelpText = "The list of files to pack (if omitted, all files are packed)")]
        [UsedImplicitly]
        public IEnumerable<string> Files { get; set; }
    }
}