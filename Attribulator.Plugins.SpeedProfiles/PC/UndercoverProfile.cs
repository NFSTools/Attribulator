using System.Collections.Generic;
using System.IO;
using System.Linq;
using Attribulator.API;
using Attribulator.API.Data;
using CoreLibraries.IO;
using VaultLib.Core;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.DB;
using VaultLib.Core.Pack;

namespace Attribulator.Plugins.SpeedProfiles.PC
{
    /// <summary>
    ///     Basic profile for PC 32-bit NFS Undercover
    /// </summary>
    public class UndercoverProfile : IProfile<Key32>
    {
        public Database<Key32> CreateDatabase()
        {
            var module = new VaultLib.Support.Undercover.ModuleDef();
            var typeRegistryBuilder = new TypeRegistryBuilder<Key32>(ByteOrder.Little);
            module.RegisterTypes(typeRegistryBuilder);
            return new Database<Key32>(module.CreateExportFactory(), typeRegistryBuilder);
        }

        public IEnumerable<LoadedFile<Key32>> LoadFiles(Database<Key32> database, string directory)
        {
            return (from file in GetFilesToLoad()
                let path = Path.Combine(directory, file)
                let standardVaultPack = new StandardVaultPack()
                let br = new BinaryReader(File.OpenRead(path))
                let vaults = standardVaultPack.Load(br, database, new PackLoadingOptions())
                select new LoadedFile<Key32>(Path.GetFileNameWithoutExtension(file), "main", vaults)).ToList();
        }

        public void SaveFiles(Database<Key32> database, string directory, IEnumerable<LoadedFile<Key32>> files)
        {
            foreach (var file in files)
            {
                IVaultPack vaultPack = new StandardVaultPack();

                //var standardVaultPack = new StandardVaultPack();
                Directory.CreateDirectory(Path.Combine(directory, file.Group));
                var outPath = Path.Combine(directory, file.Group, file.Name + ".bin");
                using var bw = new BinaryWriter(File.Open(outPath, FileMode.Create, FileAccess.ReadWrite));
                vaultPack.Save(bw, file.Vaults.ToList(), new PackSavingOptions(vaultWriteOptions: new VaultWriteOptions
                {
                    Quirks = new VaultWriteQuirks
                    {
                        StartChunkBeforeDepChunk = true
                    }
                }));
                bw.Close();

                if (file.Name == "gameplay")
                {
                    using var outStream = new FileStream(Path.ChangeExtension(outPath, "lzc"), FileMode.Create,
                        FileAccess.Write);
                    using var inStream = new FileStream(outPath, FileMode.Open, FileAccess.Read);
                    using var outWriter = new BinaryWriter(outStream);
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

        public string GetName()
        {
            return "Need for Speed Undercover";
        }

        public string GetGameId()
        {
            return "UNDERCOVER";
        }

        public string GetProfileId()
        {
            return "UNDERCOVER";
        }

        private static IEnumerable<string> GetFilesToLoad()
        {
            return new[] { "attributes.bin", "cars_vault.bin", "fe_attrib.bin", "gameplay.bin" };
        }
    }
}