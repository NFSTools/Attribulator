using CommandLine;
using CoreLibraries.ModuleSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VaultLib.Core.DB;
using VaultLib.Core.Hashing;
using VaultLib.Core.Utils;
using YAMLDatabase.ModScript;
using YAMLDatabase.Profiles;

namespace YAMLDatabase
{
    enum OperationMode
    {
        Unpack,
        Pack,
        ApplyModScript
    }

    class ProgramArgs
    {
        [Option('i', HelpText = "Directory to read files (.yml or .bin) from", Required = true)]
        public string InputDirectory { get; set; }

        [Option('o', HelpText = "Directory to write files (.yml or .bin) to", Required = true)]
        public string OutputDirectory { get; set; }

        [Option('m', HelpText = "Mode to run the program in", Required = true)]
        public OperationMode Mode { get; set; }

        [Option('p', HelpText = "The profile to use", Required = true)]
        public string ProfileName { get; set; }

        [Option('s', HelpText = "The path to the .nfsms file")]
        public string ModScriptPath { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<ProgramArgs>(args)
                .WithParsed(RunProgram);
        }

        private static void RunProgram(ProgramArgs args)
        {
            if (!Directory.Exists(args.InputDirectory))
            {
                throw new Exception($"Non-existent input directory: {args.InputDirectory}");
            }

            if (!Directory.Exists(args.OutputDirectory))
            {
                Directory.CreateDirectory(args.OutputDirectory);
            }

            new ModuleLoader("VaultLib.Support.*.dll").Load();
            HashManager.LoadDictionary("hashes.txt");

            List<BaseProfile> profiles = new List<BaseProfile>
            {
                new CarbonProfile(),

                // TODO: add more profiles
            };

            BaseProfile profile = profiles.Find(p => p.GetName() == args.ProfileName);

            if (profile == null)
            {
                throw new Exception("Unknown profile: " + args.ProfileName);
            }

            switch (args.Mode)
            {
                case OperationMode.Pack:
                    RunPack(args, profile);
                    break;
                case OperationMode.Unpack:
                    RunUnpack(args, profile);
                    break;
                case OperationMode.ApplyModScript:
                    RunApplyModScript(args, profile);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void RunApplyModScript(ProgramArgs args, BaseProfile profile)
        {
            if (string.IsNullOrEmpty(args.ModScriptPath))
            {
                throw new Exception("Missing modscript path!");
            }

            var database = new Database(new DatabaseOptions(profile.GetGame(), profile.GetDatabaseType()));
            var deserializer = new DatabaseDeserializer(database, args.InputDirectory);

            Stopwatch stopwatch = Stopwatch.StartNew();
            deserializer.Deserialize();
            stopwatch.Stop();

            Debug.WriteLine("Loaded database from {2} in {0}ms ({1:f2}s)", stopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds / 1000f, args.InputDirectory);

            stopwatch.Restart();

            var modScriptParser = new ModScriptParser(args.ModScriptPath);

            foreach (var command in modScriptParser.Parse())
            {
                command.Execute(database);
            }
            Debug.WriteLine("Applied script from {2} in {0}ms ({1:f2}s)", stopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds / 1000f, args.ModScriptPath);
            stopwatch.Stop();

            foreach (var collection in database.RowManager.GetFlattenedCollections())
            {
                foreach (var dataPair in collection.GetData())
                {
                    if (dataPair.Value is IReferencesStrings stringReferencer)
                    {
                        foreach (var s in stringReferencer.GetStrings())
                        {
                            if (s == null)
                                throw new Exception(
                                    $"collection {collection.ShortPath} field {dataPair.Key} has a null string!");
                        }
                    }
                }
            }

            stopwatch.Restart();
            deserializer.GenerateFiles(profile, args.OutputDirectory);
            stopwatch.Stop();

            Debug.WriteLine("Exported VLT files to {2} in {0}ms ({1:f2}s)", stopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds / 1000f, args.OutputDirectory);
        }

        private static void RunUnpack(ProgramArgs args, BaseProfile profile)
        {
            var database = new Database(new DatabaseOptions(profile.GetGame(), profile.GetDatabaseType()));
            var files = profile.LoadFiles(database, args.InputDirectory);
            database.CompleteLoad();

            var stopwatch = Stopwatch.StartNew();

            var serializer = new DatabaseSerializer(database, args.OutputDirectory);
            serializer.Serialize(files);

            stopwatch.Stop();

            Debug.WriteLine("Exported database to {2} in {0}ms ({1:f2}s)", stopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds / 1000f, args.OutputDirectory);
        }

        private static void RunPack(ProgramArgs args, BaseProfile profile)
        {
            var database = new Database(new DatabaseOptions(profile.GetGame(), profile.GetDatabaseType()));
            var deserializer = new DatabaseDeserializer(database, args.InputDirectory);

            Stopwatch stopwatch = Stopwatch.StartNew();
            deserializer.Deserialize();
            stopwatch.Stop();

            Debug.WriteLine("Loaded database from {2} in {0}ms ({1:f2}s)", stopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds / 1000f, args.InputDirectory);

            stopwatch.Restart();
            deserializer.GenerateFiles(profile, args.OutputDirectory);
            stopwatch.Stop();

            Debug.WriteLine("Exported VLT files to {2} in {0}ms ({1:f2}s)", stopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds / 1000f, args.OutputDirectory);
        }
    }
}
