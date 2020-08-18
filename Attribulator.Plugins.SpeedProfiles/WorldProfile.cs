using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Attribulator.API;
using Attribulator.API.Data;
using Attribulator.Plugins.SpeedProfiles.World;
using VaultLib.Core.DB;
using VaultLib.Core.Pack;

namespace Attribulator.Plugins.SpeedProfiles
{
    public class WorldProfile : IProfile
    {
        public IEnumerable<LoadedFile> LoadFiles(Database database, string directory)
        {
            var files = new List<LoadedFile>();
            foreach (var file in GetFilesToLoad(directory))
            {
                //var standardVaultPack = new StandardVaultPack();
                using var br = new BinaryReader(File.OpenRead(file));

                IVaultPack vaultPack = new StandardVaultPack();
                var group = "main";

                if (file.Contains("gc.vaults"))
                {
                    vaultPack = new GameplayVault(null);
                    group = "gameplay";
                }

                var vaults = vaultPack.Load(br, database, new PackLoadingOptions());

                files.Add(new LoadedFile(Path.GetFileNameWithoutExtension(file), group, vaults));
            }

            return files;
        }

        public void SaveFiles(Database database, string directory, IEnumerable<LoadedFile> files)
        {
            foreach (var file in files)
            {
                var vaultsToSave = file.Vaults.ToList();

                IVaultPack vaultPack = new StandardVaultPack();

                if (file.Group == "gameplay")
                    vaultPack = new GameplayVault(file.Name);

                //var standardVaultPack = new StandardVaultPack();
                Directory.CreateDirectory(Path.Combine(directory, file.Group));
                var outPath = Path.Combine(directory, file.Group, file.Name + ".bin");
                Debug.WriteLine("Saving file '{0}' to '{1}' ({2} vaults)", file.Name, outPath, vaultsToSave.Count);
                using var bw = new BinaryWriter(File.Open(outPath, FileMode.Create, FileAccess.ReadWrite));
                vaultPack.Save(bw, vaultsToSave, new PackSavingOptions());
                bw.Close();
            }
        }

        public string GetName()
        {
            return "Need for Speed World";
        }

        public string GetGameId()
        {
            return "WORLD";
        }

        public string GetProfileId()
        {
            return "WORLD";
        }

        public DatabaseType GetDatabaseType()
        {
            return DatabaseType.X86Database;
        }

        private static IEnumerable<string> GetFilesToLoad(string directory)
        {
            yield return Path.Combine(directory, "attributes.bin");
            yield return Path.Combine(directory, "commerce.bin");
            yield return Path.Combine(directory, "fe_attrib.bin");

            foreach (var file in Directory.GetFiles(Path.Combine(directory, "gc.vaults"), "*.bin",
                SearchOption.TopDirectoryOnly))
                yield return file;
        }
    }
}