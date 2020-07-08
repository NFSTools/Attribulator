using System.IO;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.Types;

namespace YAMLDatabase.Plugins.BPSupport.Types
{
    public class RwVector3 : VLTBaseType
    {
        public RwVector3(VltClass @class, VltClassField field, VltCollection collection) : base(@class, field,
            collection)
        {
        }

        public RwVector3(VltClass @class, VltClassField field) : base(@class, field)
        {
        }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public override void Read(Vault vault, BinaryReader br)
        {
            X = br.ReadSingle();
            Y = br.ReadSingle();
            Z = br.ReadSingle();
            br.ReadUInt32();
        }

        public override void Write(Vault vault, BinaryWriter bw)
        {
            bw.Write(X);
            bw.Write(Y);
            bw.Write(Z);
            bw.Write(0);
        }
    }
}