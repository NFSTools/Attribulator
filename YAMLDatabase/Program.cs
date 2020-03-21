using CommandLine;
using CoreLibraries.ModuleSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VaultLib.Core.DB;
using VaultLib.Core.Hashing;
using VaultLib.Frameworks.Speed;
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
                new MostWantedProfile(),
                new CarbonProfile(),
                new ProStreetProfile(),
                new UndercoverProfile(),
                new WorldProfile(),
            };

            BaseProfile profile = profiles.Find(p => p.GetName() == args.ProfileName);

            if (profile == null)
            {
                Console.WriteLine("ERROR: Unknown profile {0}", args.ProfileName);
                Console.WriteLine("AVAILABLE PROFILES:");
                foreach (var baseProfile in profiles)
                {
                    Console.WriteLine("\tNAME: {0}", baseProfile.GetName());
                }

                return;
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
            var loadedDatabase = deserializer.Deserialize();
            stopwatch.Stop();

            Console.WriteLine("Loaded database from {2} in {0}ms ({1:f2}s)", stopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds / 1000f, args.InputDirectory);

            stopwatch.Restart();

            var modScriptParser = new ModScriptParser(args.ModScriptPath);
            var cmdStopwatch = Stopwatch.StartNew();
            var modScriptDatabase = new ModScriptDatabaseHelper(database);

            foreach (var command in modScriptParser.Parse())
            {
                try
                {
                    cmdStopwatch.Restart();
                    command.Execute(modScriptDatabase);
                    //Console.WriteLine("Executed command in {1}ms: {0}", command.Line, cmdStopwatch.ElapsedMilliseconds);
                }
                catch (Exception e)
                {
                    throw new ModScriptCommandExecutionException($"Failed to execute command: {command.Line}", e);
                }
            }
            stopwatch.Stop();
            Console.WriteLine("Applied script from {2} in {0}ms ({1:f2}s)", stopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds / 1000f, args.ModScriptPath);

            stopwatch.Restart();
            Console.WriteLine("Making backup");
            Directory.Move(args.InputDirectory, $"{args.InputDirectory.TrimEnd('/', '\\')}_{DateTimeOffset.Now.ToUnixTimeSeconds()}");
            Directory.CreateDirectory(args.InputDirectory);
            stopwatch.Stop();
            Console.WriteLine("Made backup in {0}ms ({1:f2}s)", stopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds / 1000f);

            stopwatch.Restart();
            new DatabaseSerializer(database, args.InputDirectory).Serialize(loadedDatabase.Files);

            //deserializer.GenerateFiles(profile, args.OutputDirectory);
            stopwatch.Stop();

            Console.WriteLine("Exported YML files to {2} in {0}ms ({1:f2}s)", stopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds / 1000f, args.InputDirectory);

            stopwatch.Restart();
            deserializer.GenerateFiles(profile, args.OutputDirectory);
            stopwatch.Stop();

            Console.WriteLine("Exported VLT files to {2} in {0}ms ({1:f2}s)", stopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds / 1000f, args.OutputDirectory);
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
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

            Console.WriteLine("Exported database to {2} in {0}ms ({1:f2}s)", stopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds / 1000f, args.OutputDirectory);
        }

        private static void RunPack(ProgramArgs args, BaseProfile profile)
        {
            var database = new Database(new DatabaseOptions(profile.GetGame(), profile.GetDatabaseType()));
            var deserializer = new DatabaseDeserializer(database, args.InputDirectory);

            Stopwatch stopwatch = Stopwatch.StartNew();
            deserializer.Deserialize();
            stopwatch.Stop();

            Console.WriteLine("Loaded database from {2} in {0}ms ({1:f2}s)", stopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds / 1000f, args.InputDirectory);

            stopwatch.Restart();
            deserializer.GenerateFiles(profile, args.OutputDirectory);
            stopwatch.Stop();
            Console.WriteLine("Exported VLT files to {2} in {0}ms ({1:f2}s)", stopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds / 1000f, args.OutputDirectory);
        }
    }
}
