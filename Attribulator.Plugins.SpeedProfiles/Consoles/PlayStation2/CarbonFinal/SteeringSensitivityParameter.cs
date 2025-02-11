using System.Buffers.Binary;
using System.Runtime.InteropServices;
using VaultLib.Core.Types;
using VaultLib.Core.Utils;

namespace Attribulator.Plugins.SpeedProfiles.Consoles.PlayStation2.CarbonFinal;

[VltTypeInfo(nameof(SteeringSensitivityParameter))]
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct SteeringSensitivityParameter : IComplexType
{
    public uint Unknown1;
    public float Unknown2;
    public float Unknown3;
    public float Unknown4;
    public uint Unknown5;

    public void EndianSwap()
    {
        Unknown1 = BinaryPrimitives.ReverseEndianness(Unknown1);
        Unknown2 = Unknown2.EndianSwap();
        Unknown3 = Unknown3.EndianSwap();
        Unknown4 = Unknown4.EndianSwap();
        Unknown5 = BinaryPrimitives.ReverseEndianness(Unknown5);
    }

    public object Clone()
    {
        return MemberwiseClone();
    }
}