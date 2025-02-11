using System;
using System.IO;
using VaultLib.Core;
using VaultLib.Core.DataInterfaces;

namespace Attribulator.Plugins.SpeedProfiles.Consoles.X360.CarbonDemo;

internal class CarbonX360ExportEntry : IExportEntry<Key32>
{
    public void Read(VaultReadContext<Key32> context, BinaryReader br)
    {
        Id = Key32.Read(br);
        Type = Key32.Read(br);
        if (br.ReadUInt32() != 0)
            throw new InvalidDataException();
        Size = br.ReadUInt32();
        Offset = br.ReadUInt32();
    }

    public void Write(VaultWriteContext<Key32> context, BinaryWriter bw)
    {
        throw new NotImplementedException();
    }

    public Key32 Id { get; set; }
    public Key32 Type { get; set; }
    public uint Size { get; set; }
    public uint Offset { get; set; }
}