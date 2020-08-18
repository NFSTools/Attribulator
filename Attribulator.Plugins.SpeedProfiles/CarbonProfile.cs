using System.Collections.Generic;
using System.IO;
using System.Linq;
using Attribulator.API;
using Attribulator.API.Data;
using VaultLib.Core.DB;
using VaultLib.Core.Pack;

namespace Attribulator.Plugins.SpeedProfiles
{
    /// <summary>
    ///     Basic profile for PC 32-bit NFS Carbon
    /// </summary>
    public class CarbonProfile : IProfile
    {
        public IEnumerable<LoadedFile> LoadFiles(Database database, string directory)
        {
            return (from file in GetFilesToLoad()
                let path = Path.Combine(directory, file)
                let standardVaultPack = new StandardVaultPack()
                let br = new BinaryReader(File.OpenRead(path))
                let vaults = standardVaultPack.Load(br, database, new PackLoadingOptions())
                select new LoadedFile(Path.GetFileNameWithoutExtension(file), "main", vaults)).ToList();
        }

        public void SaveFiles(Database database, string directory, IEnumerable<LoadedFile> files)
        {
            foreach (var file in files)
            {
                IVaultPack vaultPack = new StandardVaultPack();

                //var standardVaultPack = new StandardVaultPack();
                Directory.CreateDirectory(Path.Combine(directory, file.Group));
                var outPath = Path.Combine(directory, file.Group, file.Name + ".bin");
                using var bw = new BinaryWriter(File.Open(outPath, FileMode.Create, FileAccess.ReadWrite));
                vaultPack.Save(bw, file.Vaults.ToList(), new PackSavingOptions());
                bw.Close();

                if (file.Name == "gameplay")
                {
                    using var outStream = new FileStream(Path.ChangeExtension(outPath, "lzc"), FileMode.Create,
                        FileAccess.Write);
                    using var inStream = new FileStream(outPath, FileMode.Open, FileAccess.Read);
                    using var outWriter = new BinaryWriter(outStream);
                    outWriter.Write(0x57574152); // RAWW
                    outWriter.Write((byte) 0x01);
                    outWriter.Write((byte) 0x10);
                    outWriter.Write((ushort) 0);
                    outWriter.Write((int) inStream.Length);
                    outWriter.Write((int) (inStream.Length + 16));
                    inStream.CopyTo(outStream);
                }
            }
        }

        public string GetName()
        {
            return "Need for Speed Carbon";
        }

        public string GetGameId()
        {
            return "CARBON";
        }

        public string GetProfileId()
        {
            return "CARBON";
        }

        public DatabaseType GetDatabaseType()
        {
            return DatabaseType.X86Database;
        }

        private static IEnumerable<string> GetFilesToLoad()
        {
            return new[] {"attributes.bin", "fe_attrib.bin", "gameplay.bin"};
        }
    }
}