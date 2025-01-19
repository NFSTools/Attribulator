using System.IO;
using VaultLib.Core;
using VaultLib.Core.Types;

namespace Attribulator.Plugins.BPSupport.Types
{
    public class RwVector2 : VltBaseType
    {
        public float X { get; set; }
        public float Y { get; set; }

        public override void Read(VaultReadContext context, FieldReadWriteContext fieldContext, BinaryReader br)
        {
            X = br.ReadSingle();
            Y = br.ReadSingle();
        }

        public override void Write(VaultWriteContext context, FieldReadWriteContext fieldContext, BinaryWriter bw)
        {
            bw.Write(X);
            bw.Write(Y);
        }
    }
}