using CommandLine;
using CoreLibraries.ModuleSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using VaultLib.Core.DB;
using VaultLib.Core.Hashing;
using YAMLDatabase.Core;
using YAMLDatabase.ModScript;
using YAMLDatabase.Profiles;

namespace YAMLDatabase
{
    internal static class Program
    {
        private static readonly List<BaseProfile> Profiles = new List<BaseProfile>
        {
            new MostWantedProfile(),
            new CarbonProfile(),
            new ProStreetProfile(),
            new UndercoverProfile(),
            new WorldProfile(),
        };

        static int Main(string[] args)
        {
            new ModuleLoader("VaultLib.Support.*.dll").Load();
            HashManager.LoadDictionary(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hashes.txt"));
            
            return Parser.Default.ParseArguments<UnpackOptions, PackOptions, ApplyScriptOptions>(args)
                .MapResult(
                    (UnpackOptions opts) => RunUnpack(opts), 
                    (PackOptions opts) => RunPack(opts), 
                    (ApplyScriptOptions opts) => RunApplyModScript(opts), 
                    errs => 1);
        }

        private static BaseProfile ResolveProfile(string name)
        {
            return Profiles.Find(p => p.GetName() == name);
        }

        private static int RunUnpack(UnpackOptions args)
        {
            if (!Directory.Exists(args.InputDirectory))
            {
                throw new Exception($"Non-existent input directory: {args.InputDirectory}");
            }

            if (!Directory.Exists(args.OutputDirectory))
            {
                Directory.CreateDirectory(args.OutputDirectory);
            }
            
            var profile = ResolveProfile(args.ProfileName);

            if (profile == null)
            {
                Console.WriteLine("ERROR: Unknown profile {0}", args.ProfileName);
                Console.WriteLine("AVAILABLE PROFILES:");
                foreach (var baseProfile in Profiles)
                {
                    Console.WriteLine("\tNAME: {0}", baseProfile.GetName());
                }

                return 1;
            }

            var database = new Database(new DatabaseOptions(profile.GetGame(), profile.GetDatabaseType()));
            var files = profile.LoadFiles(database, args.InputDirectory);
            database.CompleteLoad();
            var stopwatch = Stopwatch.StartNew();

            var serializer = new DatabaseSerializer(database, args.OutputDirectory);
            serializer.Serialize(files);

            stopwatch.Stop();

            Console.WriteLine("Exported database to {2} in {0}ms ({1:f2}s)", stopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds / 1000f, args.OutputDirectory);

            return 0;
        }

        private static int RunPack(PackOptions args)
        {
            if (!Directory.Exists(args.InputDirectory))
            {
                throw new Exception($"Non-existent input directory: {args.InputDirectory}");
            }

            if (!Directory.Exists(args.OutputDirectory))
            {
                Directory.CreateDirectory(args.OutputDirectory);
            }
            
            var profile = ResolveProfile(args.ProfileName);

            if (profile == null)
            {
                Console.WriteLine("ERROR: Unknown profile {0}", args.ProfileName);
                Console.WriteLine("AVAILABLE PROFILES:");
                foreach (var baseProfile in Profiles)
                {
                    Console.WriteLine("\tNAME: {0}", baseProfile.GetName());
                }

                return 1;
            }

            var database = new Database(new DatabaseOptions(profile.GetGame(), profile.GetDatabaseType()));
            var deserializer = new DatabaseDeserializer(database, args.InputDirectory);

            Stopwatch stopwatch = Stopwatch.StartNew();
            deserializer.Deserialize();
            stopwatch.Stop();

            Console.WriteLine("Loaded database from {2} in {0}ms ({1:f2}s)", stopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds / 1000f, args.InputDirectory);

            stopwatch.Restart();
            deserializer.GenerateFiles(profile, args.OutputDirectory, args.Files);
            stopwatch.Stop();
            Console.WriteLine("Exported VLT files to {2} in {0}ms ({1:f2}s)", stopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds / 1000f, args.OutputDirectory);
            return 0;
        }

        private static int RunApplyModScript(ApplyScriptOptions args)
        {
            if (!Directory.Exists(args.InputDirectory))
            {
                throw new Exception($"Non-existent input directory: {args.InputDirectory}");
            }

            if (!Directory.Exists(args.OutputDirectory))
            {
                Directory.CreateDirectory(args.OutputDirectory);
            }
            
            if (string.IsNullOrEmpty(args.ModScriptPath))
            {
                throw new Exception("Missing modscript path!");
            }
            
            var profile = ResolveProfile(args.ProfileName);

            if (profile == null)
            {
                Console.WriteLine("ERROR: Unknown profile {0}", args.ProfileName);
                Console.WriteLine("AVAILABLE PROFILES:");
                foreach (var baseProfile in Profiles)
                {
                    Console.WriteLine("\tNAME: {0}", baseProfile.GetName());
                }

                return 1;
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
            int commandCount = 0;

            foreach (var command in modScriptParser.Parse())
            {
#if !DEBUG
                try
                {
#endif
                cmdStopwatch.Restart();
                command.Execute(modScriptDatabase);
                commandCount++;
                //Console.WriteLine("Executed command in {1}ms: {0}", command.Line, cmdStopwatch.ElapsedMilliseconds);
#if !DEBUG
                }
                catch (Exception e)
                {
                    throw new ModScriptCommandExecutionException($"Failed to execute command: {command.Line}", e);
                }
#endif
            }

            stopwatch.Stop();
            float commandsPerSecond = commandCount / (stopwatch.ElapsedMilliseconds / 1000.0f);
            Console.WriteLine("Applied script from {2} in {0}ms ({1:f2}s) ({4} commands @ ~{3:f2} commands/sec)",
                stopwatch.ElapsedMilliseconds,
                stopwatch.ElapsedMilliseconds / 1000f, args.ModScriptPath, commandsPerSecond, commandCount);
            stopwatch.Restart();
            Console.WriteLine("Making backup");
            Directory.Move(args.InputDirectory,
                $"{args.InputDirectory.TrimEnd('/', '\\')}_{DateTimeOffset.Now.ToUnixTimeSeconds()}");
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

            return 0;
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
    }
}