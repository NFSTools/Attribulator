using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using CoreLibraries.IO;
using VaultLib.Core;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.DB;
using VaultLib.Core.Pack;

namespace Attribulator.Plugins.BPSupport
{
    public class BurnoutVaultPack : IVaultPack
    {
        private readonly string _vaultName;

        public BurnoutVaultPack(string vaultName)
        {
            _vaultName = vaultName;
        }

        public IList<Vault<TKey>> Load<TKey>(BinaryReader br, Database<TKey> database,
            PackLoadingOptions loadingOptions = null) where TKey : struct, IKey<TKey>
        {
            var vltOffset = br.ReadUInt32();
            var vltSize = br.ReadUInt32();
            var binOffset = br.ReadUInt32();
            var binSize = br.ReadUInt32();

            if (vltOffset > br.BaseStream.Length)
                throw new InvalidDataException();

            if (binOffset > br.BaseStream.Length)
                throw new InvalidDataException();

            br.BaseStream.Position = vltOffset;
            var vltData = new byte[vltSize];

            if (br.Read(vltData) != vltData.Length) throw new InvalidDataException();

            br.BaseStream.Position = binOffset;
            var binData = new byte[binSize];

            if (br.Read(binData) != binData.Length) throw new InvalidDataException();

            var byteOrder = loadingOptions?.ByteOrder ?? ByteOrder.Little;

            var binStream = new MemoryStream(binData);
            var vltStream = new MemoryStream(vltData);
            using var loadingWrapper = new VaultReadWrapper(_vaultName, binStream, vltStream,
                byteOrder);
            var vault = database.LoadVault(loadingWrapper);
            return new ReadOnlyCollection<Vault<TKey>>(new List<Vault<TKey>>(new[] { vault }));
        }

        public void Save<TKey>(BinaryWriter bw, IList<Vault<TKey>> vaults, PackSavingOptions savingOptions)
            where TKey : struct, IKey<TKey>
        {
            bw.Write(0x10);
            var vault = vaults[0];
            var vw = new VaultWriter<TKey>(vault, new VaultWriteOptions() { HashMode = VaultHashMode.Hash64 });
            var streamInfo = vw.BuildVault();
            bw.Write((uint)streamInfo.VltStream.Length);
            bw.Write(0);
            bw.Write((uint)streamInfo.BinStream.Length);

            streamInfo.VltStream.CopyTo(bw.BaseStream);
            bw.AlignWriter(0x10);
            var binOffset = bw.BaseStream.Position;
            streamInfo.BinStream.CopyTo(bw.BaseStream);
            var endOffset = bw.BaseStream.Position;

            bw.BaseStream.Position = 8;
            bw.Write((uint)binOffset);
            bw.BaseStream.Position = endOffset;
        }
    }
}