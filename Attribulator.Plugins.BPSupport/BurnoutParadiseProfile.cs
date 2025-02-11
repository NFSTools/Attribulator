using System.Collections.Generic;
using System.IO;
using System.Linq;
using Attribulator.API;
using Attribulator.API.Data;
using Attribulator.Plugins.BPSupport.Types;
using CoreLibraries.IO;
using VaultLib.Core;
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
            var typeRegistryBuilder = new TypeRegistryBuilder<Key64>(ByteOrder.Little);
            typeRegistryBuilder.RegisterStruct<RwVector2>("Attrib::Types::RwVector2");
            typeRegistryBuilder.RegisterStruct<RwVector3>("Attrib::Types::RwVector3");
            typeRegistryBuilder.Map<int>("AttribSys::Enums::eSongHint::eSongHint");
            typeRegistryBuilder.Map<int>("AttribSys::Enums::eCollisionMixerSliders::eCollisionMixerSliders");
            typeRegistryBuilder.Map<int>("AttribSys::Enums::PresentationAction::PresentationAction");
            typeRegistryBuilder.Map<int>("AttribSys::Enums::eShiftTypes::eShiftTypes");
            typeRegistryBuilder.Map<int>("AttribSys::Enums::eSampleTags::eSampleTags");
            typeRegistryBuilder.Map<int>("AttribSys::Enums::eReverbTypes::eReverbTypes");
            typeRegistryBuilder.Map<int>("AttribSys::Enums::ePassbyTypes::ePassbyTypes");
            typeRegistryBuilder.Map<int>("AttribSys::Enums::ProceduralShotType::ProceduralShotType");
            typeRegistryBuilder.Map<int>("AttribSys::Enums::ProceduralShakeMethod::ProceduralShakeMethod");
            typeRegistryBuilder.Map<int>("AttribSys::Enums::ParticleBlend::ParticleBlend");
            typeRegistryBuilder.Map<int>("AttribSys::Enums::NativeParticleType::NativeParticleType");
            typeRegistryBuilder.Map<int>("AttribSys::Enums::CarState::CarState");
            typeRegistryBuilder.Map<int>("AttribSys::Enums::County::County");
            typeRegistryBuilder.Map<int>("AttribSys::Enums::OffenceType::OffenceType");

            return new Database<Key64>(
                new ExportFactory<Key64>(() => new DatabaseLoad32On64(), () => new ClassLoad64(),
                    () => new CollectionLoad64(),
                    () => new ExportEntry64(), () => new PtrRef64()),
                typeRegistryBuilder
            );
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
    }
}