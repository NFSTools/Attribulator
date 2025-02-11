using System.Collections.Generic;
using System.IO;
using System.Linq;
using Attribulator.API;
using Attribulator.API.Data;
using Attribulator.Plugins.SpeedProfiles.Consoles.X360.CarbonDemo;
using CoreLibraries.IO;
using VaultLib.Core;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.DB;
using VaultLib.Core.Exports;
using VaultLib.Core.Exports.Implementations;
using VaultLib.Core.Pack;
using VaultLib.Core.Types.Attrib;

namespace Attribulator.Plugins.SpeedProfiles.Consoles.X360
{
    /// <summary>
    ///     Basic profile for X360 32-bit NFS Carbon DEMO
    /// </summary>
    public class CarbonDemoProfile : IProfile<Key32>
    {
        public Database<Key32> CreateDatabase()
        {
            var module = new VaultLib.Support.Carbon.ModuleDef32();
            var typeRegistryBuilder = new TypeRegistryBuilder<Key32>(ByteOrder.Big);

            module.RegisterTypes(typeRegistryBuilder);
            typeRegistryBuilder.Register<CarbonDemo.ControllerDataRecord>("ControllerDataRecord");
            typeRegistryBuilder.Register<StringKey64>("Attrib::StringKey");

            var exportFactory = new ExportFactory<Key32>(() => new DatabaseLoad(), () => new CarbonX360ClassLoad(),
                () => new VaultLib.ModernBase.Exports.CollectionLoad(),
                () => new CarbonX360ExportEntry());
            return new Database<Key32>(
                exportFactory,
                typeRegistryBuilder
            );
        }

        public IEnumerable<LoadedFile<Key32>> LoadFiles(Database<Key32> database, string directory)
        {
            return (from file in GetFilesToLoad()
                let path = Path.Combine(directory, file)
                let standardVaultPack = new StandardVaultPack()
                let br = new BinaryReader(File.OpenRead(path))
                let vaults = standardVaultPack.Load(br, database, new PackLoadingOptions(ByteOrder.Big))
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
                vaultPack.Save(bw, file.Vaults.ToList(), new PackSavingOptions());
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
            return "Need for Speed Carbon DEMO (Xbox 360)";
        }

        public string GetGameId()
        {
            return "CARBON";
        }

        public string GetProfileId()
        {
            return "CARBON_X360_DEMO";
        }

        private static IEnumerable<string> GetFilesToLoad()
        {
            return new[] { "attributes.bin", "fe_attrib.bin", "gameplay.bin" };
        }
    }
}