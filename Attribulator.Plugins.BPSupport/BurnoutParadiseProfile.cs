using System.Collections.Generic;
using System.IO;
using System.Linq;
using Attribulator.API;
using Attribulator.API.Data;
using Attribulator.Plugins.BPSupport.Types;
using VaultLib.Core.DB;
using VaultLib.Core.Exports;
using VaultLib.Core.Exports.Implementations;
using VaultLib.Core.Pack;
using VaultLib.Core.Types.EA.Reflection;
using VaultLib.ModernBase.Exports;
using VaultLib.ModernBase.Structures;

namespace Attribulator.Plugins.BPSupport
{
    public class BurnoutParadiseProfile : IProfile
    {
        public Database CreateDatabase()
        {
            var database = new Database(new DatabaseOptions(GetGameId(), GetDatabaseType()),
                new ExportFactory(() => new DatabaseLoad(), () => new ClassLoad64(), () => new CollectionLoad64(),
                    () => new ExportEntry64(), () => new PtrRef64()));
            database.TypeRegistry.Register<RwVector2>("Attrib::Types::RwVector2");
            database.TypeRegistry.Register<RwVector3>("Attrib::Types::RwVector3");
            database.TypeRegistry.Map<int>("AttribSys::Enums::eSongHint::eSongHint");
            database.TypeRegistry.Map<int>("AttribSys::Enums::eCollisionMixerSliders::eCollisionMixerSliders");

            return database;
        }

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

        public string GetProfileId()
        {
            return "BURNOUT_PARADISE";
        }

        public DatabaseType GetDatabaseType()
        {
            return DatabaseType.X64Database;
        }
    }
}