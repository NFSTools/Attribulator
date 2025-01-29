using System.Runtime.InteropServices;

namespace Attribulator.Plugins.BPSupport.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct RwVector3
    {
        public float X;
        public float Y;
        public float Z;
    }
}