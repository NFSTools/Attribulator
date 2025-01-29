#nullable enable
using System;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.Hashing;

namespace Attribulator.API.Utils;

public static class KeyUtils
{
    public static string KeyToString<TKey>(TKey key) where TKey : struct, IKey<TKey>
    {
        return key switch
        {
            Key32 key32 => Key32ToString(key32),
            Key64 key64 => Key64ToString(key64),
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
    
    public static string? KeyToOptString<TKey>(TKey key) where TKey : struct, IKey<TKey>
    {
        return key switch
        {
            Key32 key32 => Key32ToOptString(key32),
            Key64 key64 => Key64ToOptString(key64),
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

    public static TKey StringToKey<TKey>(string str, bool register = false) where TKey : struct, IKey<TKey>
    {
        if (typeof(TKey) == typeof(Key32))
            return (TKey)(object)StringToKey32(str, register);
        if (typeof(TKey) == typeof(Key64))
            return (TKey)(object)StringToKey64(str, register);
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

    private static string CleanResolvedString(string str)
    {
        return str.StartsWith("0x") ? $"!{str}" : str;
    }
}