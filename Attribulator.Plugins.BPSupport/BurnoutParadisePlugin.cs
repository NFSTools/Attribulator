using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Attribulator.API.Plugin;
using Attribulator.Plugins.BPSupport.Types;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.Exports;
using VaultLib.Core.Exports.Implementations;
using VaultLib.Core.Hashing;
using VaultLib.Core.Types;
using VaultLib.Core.Types.EA.Reflection;
using VaultLib.ModernBase.Exports;
using VaultLib.ModernBase.Structures;
using Int32 = VaultLib.Core.Types.EA.Reflection.Int32;

namespace Attribulator.Plugins.BPSupport
{
    public class BurnoutParadisePlugin : IPlugin
    {
        public string GetName()
        {
            return "Burnout Paradise Support";
        }

        public void Init()
        {
            HashManager.LoadDictionary(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "Resources", "hashes.txt"));
        }
    }

    [PrimitiveInfo(typeof(ulong))]
    public class UInt64 : PrimitiveTypeBase
    {
        public UInt64(VltClass @class, VltClassField field, VltCollection collection)
            : base(@class, field, collection)
        {
        }

        public UInt64(VltClass @class, VltClassField field)
            : base(@class, field)
        {
        }

        public ulong Value { get; set; }

        public override void Read(Vault vault, BinaryReader br) => this.Value = br.ReadUInt64();

        public override void Write(Vault vault, BinaryWriter bw) => bw.Write(this.Value);

        public override IConvertible GetValue() => (IConvertible)this.Value;

        public override void SetValue(IConvertible value) => this.Value = (ulong)value;
    }
}