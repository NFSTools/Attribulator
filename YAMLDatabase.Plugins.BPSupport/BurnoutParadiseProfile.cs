using System.Collections.Generic;
using System.IO;
using System.Linq;
using VaultLib.Core.DB;
using VaultLib.Core.Pack;
using YAMLDatabase.API;
using YAMLDatabase.API.Data;

namespace YAMLDatabase.Plugins.BPSupport
{
    public class BurnoutParadiseProfile : IProfile
    {
        public IEnumerable<LoadedFile> LoadFiles(Database database, string directory)
        {
            var filesToLoad = Directory.GetFiles(directory, "*.bin", SearchOption.TopDirectoryOnly)
                .Where(f => !Path.GetFileNameWithoutExtension(f).Equals("schema"))
                .ToList();
            filesToLoad.Insert(0, Path.Combine(directory, "schema.bin"));

            return (from file in filesToLoad
                let vaultPack = new BurnoutVaultPack(Path.GetFileNameWithoutExtension(file))
                let br = new BinaryReader(File.OpenRead(file))
                let vaults = vaultPack.Load(br, database, new PackLoadingOptions())
                select new LoadedFile(Path.GetFileNameWithoutExtension(file), "main", vaults)).ToList();
        }

        public void SaveFiles(Database database, string directory, IEnumerable<LoadedFile> files)
        {
            foreach (var file in files)
            {
                Directory.CreateDirectory(Path.Combine(directory, file.Group));
                IVaultPack vaultPack = new BurnoutVaultPack(file.Name);
                using var fs = new FileStream(Path.Combine(directory, file.Group, file.Name + ".bin"),
                    FileMode.Create, FileAccess.ReadWrite);
                using var bw = new BinaryWriter(fs);
                vaultPack.Save(bw, file.Vaults.ToList(), new PackSavingOptions());
            }
        }

        public string GetName()
        {
            return "Burnout Paradise";
        }

        public string GetGameId()
        {
            return "BURNOUT_PARADISE";
        }

        public DatabaseType GetDatabaseType()
        {
            return DatabaseType.X64Database;
        }
    }
}