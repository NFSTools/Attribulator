﻿using System;
using System.Collections.Generic;
using System.IO;
using CoreLibraries.IO;
using VaultLib.Core;
using VaultLib.Core.DB;
using VaultLib.Core.Pack;

namespace Attribulator.Plugins.SpeedProfiles.World
{
    public class GameplayVault : IVaultPack
    {
        private readonly string _name;

        public GameplayVault(string name)
        {
            _name = name;
        }

        public void Save(BinaryWriter bw, IList<Vault> vaults, PackSavingOptions savingOptions = null)
        {
            if (vaults.Count != 1) throw new InvalidDataException("Can only save exactly 1 vault");

            var nameChars = new char[0x2C];
            _name.CopyTo(0, nameChars, 0, _name.Length);

            var vaultWriter = new VaultWriter(vaults[0], new VaultSaveOptions());
            vaultWriter.ExportManager.AddExport(new VaultSlotExport());
            var vaultStreamInfo = vaultWriter.BuildVault();

            bw.Write(nameChars);

            var binOffsetPos = bw.BaseStream.Position;
            bw.Write(0);
            bw.Write((uint) vaultStreamInfo.BinStream.Length);
            var vltOffsetPos = bw.BaseStream.Position;
            bw.Write(0);
            bw.Write((uint) vaultStreamInfo.VltStream.Length);
            var fileSizePos = bw.BaseStream.Position;
            bw.Write(0);

            bw.AlignWriter(0x40);
            var binOffset = bw.BaseStream.Position;
            vaultStreamInfo.BinStream.CopyTo(bw.BaseStream);

            bw.AlignWriter(0x40);
            var vltOffset = bw.BaseStream.Position;
            vaultStreamInfo.VltStream.CopyTo(bw.BaseStream);
            //bw.AlignWriter(0x80);

            bw.BaseStream.Position = binOffsetPos;
            bw.Write((uint) binOffset);

            bw.BaseStream.Position = vltOffsetPos;
            bw.Write((uint) vltOffset);

            bw.BaseStream.Position = fileSizePos;
            bw.Write((uint) bw.BaseStream.Length);

            bw.BaseStream.Position = bw.BaseStream.Length;
        }

        public IList<Vault> Load(BinaryReader br, Database database, PackLoadingOptions loadingOptions)
        {
            var name = new string(br.ReadChars(0x2C)).Trim('\0');

            var binOffset = br.ReadInt32();
            var binSize = br.ReadInt32();
            var vltOffset = br.ReadInt32();
            var vltSize = br.ReadInt32();
            var fileSize = br.ReadInt32();

            if (fileSize != br.BaseStream.Length) throw new InvalidDataException("Corrupted file");

            var vault = new Vault(name);
            var byteOrder = loadingOptions?.ByteOrder ?? ByteOrder.Little;
            br.BaseStream.Seek(binOffset, SeekOrigin.Begin);
            var binBuffer = new byte[binSize];
            if (br.Read(binBuffer, 0, binBuffer.Length) != binBuffer.Length)
                throw new Exception($"Failed to read {binBuffer.Length} bytes of BIN data");
            br.BaseStream.Seek(vltOffset, SeekOrigin.Begin);
            var vltBuffer = new byte[vltSize];
            if (br.Read(vltBuffer, 0, vltBuffer.Length) != vltBuffer.Length)
                throw new Exception($"Failed to read {vltBuffer.Length} bytes of VLT data");
            vault.BinStream = new MemoryStream(binBuffer);
            vault.VltStream = new MemoryStream(vltBuffer);
            using (var loadingWrapper = new VaultLoadingWrapper(vault, byteOrder))
            {
                database.LoadVault(vault, loadingWrapper);
            }

            return new List<Vault>(new[] {vault});
        }
    }
}