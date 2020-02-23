using System.IO;
using VaultLib.Core;
using VaultLib.Core.Exports;

namespace YAMLDatabase.Profiles.World
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
            return 0x4FD71F0B;
        }

        public override string GetTypeId()
        {
            return "0x1C54EE91";
        }
    }
}