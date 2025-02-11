using System;
using System.Diagnostics;
using System.IO;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.Exports;
using VaultLib.Core.Utils;
using VaultLib.LegacyBase;

namespace Attribulator.Plugins.SpeedProfiles.Consoles.X360.CarbonDemo;

internal class CarbonX360ClassLoad : BaseClassLoad<Key32>
{
    private uint ClassHash { get; set; }
    private int NumDefinitions { get; set; }

    private uint _definitionsPtr;
    private uint _staticDataPtr;

    private long _srcDefinitionsPtr;
    private long _srcStaticPtr;
    private long _dstDefinitionsPtr;
    private long _dstStaticPtr;

    public override void Read(VaultReadContext<Key32> context, BinaryReader br)
    {
        ClassHash = br.ReadUInt32();
        var collectionReserve = br.ReadUInt32();
        var mNumDefinitions = br.ReadInt32();
        NumDefinitions = mNumDefinitions;
        _definitionsPtr = br.ReadUInt32();
        var layoutSize = br.ReadUInt32();
        var unknown1 = br.ReadUInt32();
        var unknown2 = br.ReadUInt16();
        var numLayoutFields = br.ReadUInt16();
        var unknown3 = br.ReadUInt32();
        var probablyStaticPtr = br.ReadPointer();

        if (_definitionsPtr == 0)
        {
            throw new InvalidDataException("Definitions pointer is NULL, this is not good!");
        }

        Debug.WriteLine($"CX360 class: 0x{ClassHash:X8} -- {mNumDefinitions} fields");
        Debug.WriteLine(
            $"LS = {layoutSize}; CR = {collectionReserve}; NLF = {numLayoutFields}; U1 = 0x{unknown1:X}; U2 = 0x{unknown2:X}; U3 = 0x{unknown3:X}; U4 = 0x{probablyStaticPtr:X}");
        
        Debug.Assert(probablyStaticPtr == 0, "probablyStaticPtr == 0");

        NumDefinitions = mNumDefinitions;
        Class = new VltClass<Key32>(new Key32(ClassHash))
        {
            LayoutSize = layoutSize,
            // StaticSize = staticSize,
        };
    }

    public override void Write(VaultWriteContext<Key32> context, BinaryWriter bw)
    {
        throw new NotImplementedException();
    }

    public override void ReadPointerData(VaultReadContext<Key32> context, BinaryReader br)
    {
        br.BaseStream.Position = _definitionsPtr;

        for (int i = 0; i < NumDefinitions; i++)
        {
            AttribDefinition32 definition = new AttribDefinition32();
            definition.Read(context, br);

            var field = new VltClassField<Key32>(
                Class,
                definition.Key,
                definition.Type,
                definition.Flags,
                definition.Alignment,
                definition.Size,
                definition.MaxCount,
                definition.Offset);

            Class.Fields.Add(definition.Key, field);
        }

        if (_staticDataPtr != 0)
        {
            br.BaseStream.Position = _staticDataPtr;

            foreach (var staticField in Class.StaticFields)
            {
                br.SafeAlignReader(staticField.Alignment);

                var fieldContext = new FieldReadWriteContext<Key32>(Class, staticField, null);
                var staticData =
                    context.Database.TypeRegistry.ReadFieldValue(context, fieldContext,
                        br);
                staticField.StaticValue = staticData;
            }

            var staticEndPos = br.BaseStream.Position;

            if (staticEndPos - _staticDataPtr > Class.StaticSize)
            {
                throw new Exception("read too much static data, something went wrong!");
            }
        }

        foreach (var staticField in Class.StaticFields)
        {
            var fieldContext = new FieldReadWriteContext<Key32>(Class, staticField, null);
            if (staticField.StaticValue is IVltPointerObject<Key32> vltPointerObject)
            {
                vltPointerObject.ReadPointerData(context, fieldContext, br);
            }
        }

        context.Database.AddClass(Class);
    }

    public override void WritePointerData(VaultWriteContext<Key32> context, BinaryWriter bw)
    {
        throw new NotImplementedException();
    }

    public override void AddPointers(VaultWriteContext<Key32> context)
    {
        throw new NotImplementedException();
    }

    public override Key32 GetExportId()
    {
        throw new NotImplementedException();
    }
}