using System.Collections.Generic;
using System.IO;
using System.Linq;
using Attribulator.API;
using Attribulator.API.Data;
using Attribulator.Plugins.BPSupport.Types;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.DB;
using VaultLib.Core.Exports;
using VaultLib.Core.Pack;
using VaultLib.ModernBase.Exports;
using VaultLib.ModernBase.Structures;

namespace Attribulator.Plugins.BPSupport
{
    public class BurnoutParadiseProfile : IProfile<Key64>
    {
        public Database<Key64> CreateDatabase()
        {
            var database = new Database<Key64>(new DatabaseOptions(GetGameId(), GetDatabaseType()),
                new ExportFactory<Key64>(() => new DatabaseLoad32On64(), () => new ClassLoad64(),
                    () => new CollectionLoad64(),
                    () => new ExportEntry64(), () => new PtrRef64()));
            database.TypeRegistry.RegisterStruct<RwVector2>("Attrib::Types::RwVector2");
            database.TypeRegistry.RegisterStruct<RwVector3>("Attrib::Types::RwVector3");
            database.TypeRegistry.Map<int>("AttribSys::Enums::eSongHint::eSongHint");
            database.TypeRegistry.Map<int>("AttribSys::Enums::eCollisionMixerSliders::eCollisionMixerSliders");
            database.TypeRegistry.Map<int>("AttribSys::Enums::PresentationAction::PresentationAction");
            database.TypeRegistry.Map<int>("AttribSys::Enums::eShiftTypes::eShiftTypes");
            database.TypeRegistry.Map<int>("AttribSys::Enums::eSampleTags::eSampleTags");
            database.TypeRegistry.Map<int>("AttribSys::Enums::eReverbTypes::eReverbTypes");
            database.TypeRegistry.Map<int>("AttribSys::Enums::ePassbyTypes::ePassbyTypes");
            database.TypeRegistry.Map<int>("AttribSys::Enums::ProceduralShotType::ProceduralShotType");
            database.TypeRegistry.Map<int>("AttribSys::Enums::ProceduralShakeMethod::ProceduralShakeMethod");
            database.TypeRegistry.Map<int>("AttribSys::Enums::ParticleBlend::ParticleBlend");
            database.TypeRegistry.Map<int>("AttribSys::Enums::NativeParticleType::NativeParticleType");
            database.TypeRegistry.Map<int>("AttribSys::Enums::CarState::CarState");
            database.TypeRegistry.Map<int>("AttribSys::Enums::County::County");
            database.TypeRegistry.Map<int>("AttribSys::Enums::OffenceType::OffenceType");

            return database;
        }

        public IEnumerable<LoadedFile<Key64>> LoadFiles(Database<Key64> database, string directory)
        {
            var filesToLoad = Directory.GetFiles(directory, "*.bin", SearchOption.TopDirectoryOnly)
                .Where(f => !Path.GetFileNameWithoutExtension(f).Equals("schema"))
                .ToList();
            filesToLoad.Insert(0, Path.Combine(directory, "schema.bin"));

            return (from file in filesToLoad
                let vaultPack = new BurnoutVaultPack(Path.GetFileNameWithoutExtension(file))
                let br = new BinaryReader(File.OpenRead(file))
                let vaults = vaultPack.Load(br, database, new PackLoadingOptions())
                select new LoadedFile<Key64>(Path.GetFileNameWithoutExtension(file), "main", vaults)).ToList();
        }

        public void SaveFiles(Database<Key64> database, string directory, IEnumerable<LoadedFile<Key64>> files)
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