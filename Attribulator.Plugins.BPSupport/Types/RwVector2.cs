using System.Runtime.InteropServices;

namespace Attribulator.Plugins.BPSupport.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct RwVector2
    {
        public float X;
        public float Y;
    }
}