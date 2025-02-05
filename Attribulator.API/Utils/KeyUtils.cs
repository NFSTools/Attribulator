#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.Hashing;

namespace Attribulator.API.Utils;

public static class KeyUtils
{
    private static readonly Dictionary<BinKey32, string> Bin32Dictionary = new();
    private static readonly Dictionary<BinKey64, string> Bin64Dictionary = new();

    public static void LoadBinDictionary(string path)
    {
        foreach (var line in File.ReadLines(path))
        {
            Bin32Dictionary[BinKey32.FromString(line)] = line;
            Bin64Dictionary[BinKey64.FromString(line)] = line;
        }
    }

    public static string KeyToString<TKey>(TKey key) where TKey : struct, IKey<TKey>
    {
        return key switch
        {
            Key32 key32 => Key32ToString(key32),
            Key64 key64 => Key64ToString(key64),
            BinKey32 binKey32 => BinKey32ToString(binKey32),
            BinKey64 binKey64 => BinKey64ToString(binKey64),
            _ => throw new ArgumentException("Unsupported key type", nameof(key))
        };
    }

    private static string Key32ToString(Key32 key)
    {
        var resolved = HashManager.ResolveVlt(key.Hash);

        return resolved == null ? $"0x{key.Hash:X8}" : CleanResolvedString(resolved);
    }

    private static string Key64ToString(Key64 key)
    {
        var resolved = HashManager.ResolveVlt(key.Hash);

        return resolved == null ? $"0x{key.Hash:X16}" : CleanResolvedString(resolved);
    }

    private static string BinKey32ToString(BinKey32 key)
    {
        var resolved = Bin32Dictionary.GetValueOrDefault(key);

        return resolved == null ? $"0x{key.Hash:X8}" : CleanResolvedString(resolved);
    }

    private static string BinKey64ToString(BinKey64 key)
    {
        var resolved = Bin64Dictionary.GetValueOrDefault(key);

        return resolved == null ? $"0x{key.Hash:X16}" : CleanResolvedString(resolved);
    }

    public static string? KeyToOptString<TKey>(TKey key) where TKey : struct, IKey<TKey>
    {
        return key switch
        {
            Key32 key32 => Key32ToOptString(key32),
            Key64 key64 => Key64ToOptString(key64),
            BinKey32 binKey32 => BinKey32ToOptString(binKey32),
            BinKey64 binKey64 => BinKey64ToOptString(binKey64),
            _ => throw new ArgumentException("Unsupported key type", nameof(key))
        };
    }

    private static string? Key32ToOptString(Key32 key)
    {
        return HashManager.ResolveVlt(key.Hash);
    }

    private static string? Key64ToOptString(Key64 key)
    {
        return HashManager.ResolveVlt(key.Hash);
    }

    private static string? BinKey32ToOptString(BinKey32 key)
    {
        return Bin32Dictionary.GetValueOrDefault(key);
    }

    private static string? BinKey64ToOptString(BinKey64 key)
    {
        return Bin64Dictionary.GetValueOrDefault(key);
    }

    public static TKey StringToKey<TKey>(string str, bool register = false) where TKey : struct, IKey<TKey>
    {
        if (typeof(TKey) == typeof(Key32))
            return (TKey)(object)StringToKey32(str, register);
        if (typeof(TKey) == typeof(Key64))
            return (TKey)(object)StringToKey64(str, register);
        if (typeof(TKey) == typeof(BinKey32))
            return (TKey)(object)StringToBinKey32(str, register);
        if (typeof(TKey) == typeof(BinKey64))
            return (TKey)(object)StringToBinKey64(str, register);
        throw new ArgumentException("Unsupported key type");
    }

    private static Key32 StringToKey32(string value, bool register = false)
    {
        if (value.StartsWith("!0x"))
        {
            if (register)
            {
                HashManager.AddVlt(value[1..]);
            }

            return Key32.FromString(value[1..]);
        }

        if (value.StartsWith("0x"))
        {
            return new Key32(uint.Parse(value[2..], System.Globalization.NumberStyles.HexNumber));
        }

        if (register)
        {
            HashManager.AddVlt(value);
        }

        return Key32.FromString(value);
    }

    private static Key64 StringToKey64(string value, bool register = false)
    {
        if (value.StartsWith("!0x"))
        {
            if (register)
            {
                HashManager.AddVlt(value[1..]);
            }

            return Key64.FromString(value[1..]);
        }

        if (value.StartsWith("0x"))
            return new Key64(ulong.Parse(value[2..], System.Globalization.NumberStyles.HexNumber));


        if (register)
        {
            HashManager.AddVlt(value);
        }

        return Key64.FromString(value);
    }

    private static BinKey32 StringToBinKey32(string value, bool register = false)
    {
        if (value.StartsWith("!0x"))
        {
            {
                var sliced = value[1..];
                var key = BinKey32.FromString(sliced);

                if (register)
                {
                    Bin32Dictionary[key] = sliced;
                }

                return key;
            }
        }

        if (value.StartsWith("0x"))
        {
            return new BinKey32(uint.Parse(value[2..], System.Globalization.NumberStyles.HexNumber));
        }

        {
            var key = BinKey32.FromString(value);
            if (register)
            {
                Bin32Dictionary[key] = value;
            }

            return key;
        }
    }

    private static BinKey64 StringToBinKey64(string value, bool register = false)
    {
        if (value.StartsWith("!0x"))
        {
            {
                var sliced = value[1..];
                var key = BinKey64.FromString(sliced);

                if (register)
                {
                    Bin64Dictionary[key] = sliced;
                }

                return key;
            }
        }

        if (value.StartsWith("0x"))
        {
            return new BinKey64(ulong.Parse(value[2..], System.Globalization.NumberStyles.HexNumber));
        }

        {
            var key = BinKey64.FromString(value);
            if (register)
            {
                Bin64Dictionary[key] = value;
            }

            return key;
        }
    }

    private static string CleanResolvedString(string str)
    {
        return str.StartsWith("0x") ? $"!{str}" : str;
    }
}