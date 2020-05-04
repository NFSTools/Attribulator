using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using VaultLib.Core;
using VaultLib.Core.DB;
using VaultLib.Core.Pack;
using YAMLDatabase.Core;

namespace YAMLDatabase.Profiles
{
    public class MostWantedProfile : BaseProfile
    {
        public override IList<LoadedDatabaseFile> LoadFiles(Database database, string directory)
        {
            List<LoadedDatabaseFile> files = new List<LoadedDatabaseFile>();
            foreach (var file in GetFilesToLoad(directory))
            {
                var path = Path.Combine(directory, file);
                var standardVaultPack = new StandardVaultPack();
                using var br = new BinaryReader(File.OpenRead(path));
                var vaults = standardVaultPack.Load(br, database, new PackLoadingOptions());

                var loadedDatabaseFile = new LoadedDatabaseFile
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Group = "main",
                    Vaults = vaults.Select(v => v.Name).ToList(),
                    LoadedVaults = new List<Vault>(vaults)
                };

                files.Add(loadedDatabaseFile);
            }

            return files;
        }

        public override void SaveFiles(Database database, string directory, IList<LoadedDatabaseFile> files)
        {
            foreach (var file in files)
            {
                var vaultsToSave = file.Vaults.Select(database.FindVault).ToList();

                IVaultPack vaultPack = new StandardVaultPack();

                //var standardVaultPack = new StandardVaultPack();
                Directory.CreateDirectory(Path.Combine(directory, file.Group));
                var outPath = Path.Combine(directory, file.Group, file.Name + ".bin");
                Debug.WriteLine("Saving file '{0}' to '{1}' ({2} vaults)", file.Name, outPath, vaultsToSave.Count);
                using var bw = new BinaryWriter(File.Open(outPath, FileMode.Create, FileAccess.ReadWrite));
                vaultPack.Save(bw, vaultsToSave, new PackSavingOptions());
                bw.Close();

                if (file.Name == "gameplay")
                {
                    using (FileStream outStream = new FileStream(Path.ChangeExtension(outPath, "lzc"), FileMode.Create, FileAccess.Write))
                    using (FileStream inStream = new FileStream(outPath, FileMode.Open, FileAccess.Read))
                    using (BinaryWriter outWriter = new BinaryWriter(outStream))
                    {
                        outWriter.Write(0x57574152); // RAWW
                        outWriter.Write((byte)0x01);
                        outWriter.Write((byte)0x10);
                        outWriter.Write((ushort)0);
                        outWriter.Write((int)inStream.Length);
                        outWriter.Write((int)(inStream.Length + 16));
                        inStream.CopyTo(outStream);
                    }
                }
            }
        }

        public override string GetName()
        {
            return "MOST_WANTED";
        }

        public override string GetGame()
        {
            return "MOST_WANTED";
        }

        public override DatabaseType GetDatabaseType()
        {
            return DatabaseType.X86Database;
        }

        public override IEnumerable<string> GetFilesToLoad(string directory)
        {
            return new[] {"attributes.bin", "fe_attrib.bin", "gameplay.bin"};
        }
    }
}