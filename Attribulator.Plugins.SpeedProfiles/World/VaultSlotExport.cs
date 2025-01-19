using System.IO;
using VaultLib.Core;
using VaultLib.Core.Exports;
using VaultLib.Core.Hashing;

namespace Attribulator.Plugins.SpeedProfiles.World
{
    public class VaultSlotExport : BaseExport
    {
        public override void Read(VaultReadContext context, BinaryReader br)
        {
            br.ReadUInt32();
        }

        public override void Write(VaultWriteContext context, BinaryWriter bw)
        {
            bw.Write(0);
        }

        public override ulong GetExportId()
        {
            return Vlt32Hasher.Hash("VaultData");
        }

        public override string GetTypeId()
        {
            return "VaultDataType";
        }
    }
}