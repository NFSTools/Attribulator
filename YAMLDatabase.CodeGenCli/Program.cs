using System;
using System.Diagnostics;
using System.IO;
using CommandLine;
using YAMLDatabase.CodeGenCli.Generators;
using YamlDotNet.Serialization;

namespace YAMLDatabase.CodeGenCli
{
    enum CodeLanguage
    {
        CPP,
    }

    class ProgramArgs
    {
        [Option('i', HelpText = "Directory to read .yml files from", Required = true)]
        public string InputDirectory { get; set; }

        [Option('o', HelpText = "Directory to write code files to", Required = true)]
        public string OutputDirectory { get; set; }

        [Option('l', HelpText = "Language to generate code for", Required = true)]
        public CodeLanguage Language { get; set; }
    }

    internal static class Program
    {
        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<ProgramArgs>(args)
                .WithParsed(RunProgram);
        }

        private static void RunProgram(ProgramArgs args)
        {
            var deserializer = new DeserializerBuilder().Build();
            using var dbs = new StreamReader(Path.Combine(args.InputDirectory, "info.yml"));
            var loadedDatabase = deserializer.Deserialize<LoadedDatabase>(dbs);

            Directory.CreateDirectory(args.OutputDirectory);

            ICodeGenerator generator = args.Language switch
            {
                CodeLanguage.CPP => new CppGenerator(),
                _ => throw new ArgumentOutOfRangeException()
            };

            foreach (var loadedDatabaseClass in loadedDatabase.Classes)
            {
                File.WriteAllText(
                    Path.Combine(args.OutputDirectory, loadedDatabaseClass.Name + generator.GetExtension()),
                    generator.GenerateClassLayout(loadedDatabaseClass));
            }
        }
    }
}
