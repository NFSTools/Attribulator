using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CoreLibraries.GameUtilities;
using VaultLib.Core;
using VaultLib.Core.DB;
using VaultLib.Core.Pack;
using YAMLDatabase.Core;
using YAMLDatabase.Profiles.World;

namespace YAMLDatabase.Profiles
{
    public class WorldProfile : BaseProfile
    {
        public override IList<LoadedDatabaseFile> LoadFiles(Database database, string directory)
        {
            List<LoadedDatabaseFile> files = new List<LoadedDatabaseFile>();
            foreach (var file in GetFilesToLoad(directory))
            {
                //var standardVaultPack = new StandardVaultPack();
                using var br = new BinaryReader(File.OpenRead(file));

                IVaultPack vaultPack = new StandardVaultPack();
                string group = "main";

                if (file.Contains("gc.vaults"))
                {
                    vaultPack = new GameplayVault(null);
                    group = "gameplay";
                }

                var vaults = vaultPack.Load(br, database, new PackLoadingOptions());

                var loadedDatabaseFile = new LoadedDatabaseFile
                {
                    Name = Path.GetFileNameWithoutExtension(file),
                    Group = group,
                    Vaults = vaults.Select(v => v.Name).ToList(),
                    LoadedVaults = new List<Vault>(vaults)
                };

                files.Add(loadedDatabaseFile);
            }

            return files;
        }

        public override void SaveFiles(Database database, string directory, IEnumerable<LoadedDatabaseFile> files)
        {
            foreach (var file in files)
            {
                var vaultsToSave = file.Vaults.Select(database.FindVault).ToList();

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

        public override string GetName()
        {
            return GameIdHelper.ID_WORLD;
        }

        public override string GetGame()
        {
            return GameIdHelper.ID_WORLD;
        }

        public override DatabaseType GetDatabaseType()
        {
            return DatabaseType.X86Database;
        }

        public override IEnumerable<string> GetFilesToLoad(string directory)
        {
            yield return Path.Combine(directory, "attributes.bin");
            yield return Path.Combine(directory, "commerce.bin");
            yield return Path.Combine(directory, "fe_attrib.bin");

            foreach (var file in Directory.GetFiles(Path.Combine(directory, "gc.vaults"), "*.bin", SearchOption.TopDirectoryOnly))
            {
                yield return file;
            }
        }
    }
}