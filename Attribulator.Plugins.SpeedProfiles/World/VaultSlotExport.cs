using System.IO;
using VaultLib.Core;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.Exports;
using VaultLib.Core.Hashing;

namespace Attribulator.Plugins.SpeedProfiles.World
{
    public class VaultSlotExport<TKey> : BaseExport<TKey> where TKey : struct, IKey<TKey>
    {
        public override void Read(VaultReadContext<TKey> context, BinaryReader br)
        {
            br.ReadUInt32();
        }

        public override void Write(VaultWriteContext<TKey> context, BinaryWriter bw)
        {
            bw.Write(0);
        }

        public override TKey GetExportId()
        {
            return TKey.FromString("VaultData");
        }

        public override string GetTypeId()
        {
            return "VaultDataType";
        }
    }
}