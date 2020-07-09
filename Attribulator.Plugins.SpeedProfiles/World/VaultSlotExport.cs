using System.IO;
using VaultLib.Core;
using VaultLib.Core.Exports;
using VaultLib.Core.Hashing;

namespace Attribulator.Plugins.SpeedProfiles.World
{
    public class VaultSlotExport : BaseExport
    {
        public override void Read(Vault vault, BinaryReader br)
        {
            br.ReadUInt32();
        }

        public override void Write(Vault vault, BinaryWriter bw)
        {
            bw.Write(0);
        }

        public override ulong GetExportID()
        {
            return VLT32Hasher.Hash("VaultData");
        }

        public override string GetTypeId()
        {
            return "VaultDataType";
        }
    }
}