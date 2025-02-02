using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Attribulator.API.Exceptions;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.Hashing;
using VaultLib.Core.Types;
using VaultLib.Core.Types.EA.Reflection;

namespace Attribulator.API.Utils
{
    public static class ValueConversionUtils
    {
        private static readonly Dictionary<Type, Type> TypeCache = new Dictionary<Type, Type>();

        public static object ConvertPrimitiveToNewPrimitive(Type primitiveType, string primitiveString)
        {
            if (primitiveType == typeof(string))
                return primitiveString;

            if (primitiveType.IsEnum)
            {
                // 3 acceptable input formats:
                // 1. Name (string)
                // 2. Value (decimal)
                // 3. Value (0x<hex>)

                if (!primitiveString.StartsWith("0x")) return Enum.Parse(primitiveType, primitiveString);

                if (uint.TryParse(primitiveString.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                        out var result))
                {
                    return Enum.ToObject(primitiveType, result);
                }

                throw new FormatException($"Can't interpret {primitiveString} as a hexadecimal value.");
            }

            if (primitiveType == typeof(bool))
            {
                return Convert.ToBoolean(primitiveString);
            }

            if (primitiveType == typeof(Key32))
            {
                return KeyUtils.StringToKey<Key32>(primitiveString);
            }

            if (primitiveType == typeof(Key64))
            {
                return KeyUtils.StringToKey<Key64>(primitiveString);
            }

            if (primitiveType.IsPrimitive)
            {
                return primitiveString.StartsWith("0x")
                    ? ConvertHexToPrimitive(primitiveType, primitiveString[2..])
                    : SmartHashConversion(primitiveType, primitiveString);
            }

            throw new InvalidCastException($"Can't convert input string '{primitiveString}' to {primitiveType}.");
        }

        private static object SmartHashConversion(Type primitiveType, string primitiveString)
        {
            if (primitiveType == typeof(ulong))
            {
                return ulong.TryParse(primitiveString, out var result) ? result : Vlt64Hasher.Hash(primitiveString);
            }

            if (primitiveType == typeof(long))
            {
                return long.TryParse(primitiveString, out var result)
                    ? result
                    : unchecked((long)Vlt64Hasher.Hash(primitiveString));
            }

            if (primitiveType == typeof(uint))
            {
                return uint.TryParse(primitiveString, out var result) ? result : Vlt32Hasher.Hash(primitiveString);
            }

            if (primitiveType == typeof(int))
            {
                return int.TryParse(primitiveString, out var result)
                    ? result
                    : unchecked((int)Vlt32Hasher.Hash(primitiveString));
            }

            return Convert.ChangeType(primitiveString, primitiveType, CultureInfo.InvariantCulture);
        }

        private static object ConvertHexToPrimitive(Type primitiveType, string hexString)
        {
            if (primitiveType == typeof(ulong))
                return ulong.Parse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            if (primitiveType == typeof(uint))
                return uint.Parse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            if (primitiveType == typeof(ushort))
                return ushort.Parse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            if (primitiveType == typeof(byte))
                return byte.Parse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            throw new InvalidCastException($"Can't convert hexadecimal string '{hexString}' to {primitiveType}.");
        }

        // public static object DoPrimitiveConversion(PrimitiveTypeBase primitiveTypeBase, string str)
        // {
        //     var type = primitiveTypeBase.GetType();
        //     if (TypeCache.TryGetValue(type, out var conversionType))
        //         return DoPrimitiveConversion(primitiveTypeBase, str, conversionType);
        //
        //     // Do primitive conversion
        //     var primitiveInfoAttribute =
        //         type.GetCustomAttribute<PrimitiveInfoAttribute>();
        //
        //     if (primitiveInfoAttribute == null)
        //     {
        //         // Try to determine enum type
        //         if (type.IsGenericType &&
        //             type.GetGenericTypeDefinition() == typeof(VLTEnumType<>))
        //             primitiveInfoAttribute = new PrimitiveInfoAttribute(type.GetGenericArguments()[0]);
        //         else
        //             throw new InvalidDataException("Cannot determine primitive type");
        //     }
        //
        //     var primitiveType = primitiveInfoAttribute.PrimitiveType;
        //     TypeCache[type] = primitiveType;
        //     return DoPrimitiveConversion(primitiveTypeBase, str, primitiveType);
        // }
        //
        // private static object DoPrimitiveConversion(object primitiveTypeBase, string str,
        //     Type conversionType)
        // {
        //     if (conversionType.IsEnum)
        //     {
        //         if (str.StartsWith("0x", StringComparison.Ordinal) &&
        //             uint.TryParse(str.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture,
        //                 out var val))
        //             primitiveTypeBase.SetValue((IConvertible) Enum.Parse(conversionType, val.ToString()));
        //         else
        //             primitiveTypeBase.SetValue((IConvertible) Enum.Parse(conversionType, str));
        //     }
        //     else
        //     {
        //         if (str.StartsWith("0x", StringComparison.Ordinal) && uint.TryParse(str.Substring(2),
        //             NumberStyles.AllowHexSpecifier,
        //             CultureInfo.InvariantCulture, out var val))
        //             primitiveTypeBase.SetValue((IConvertible) Convert.ChangeType(val, conversionType));
        //         else
        //             try
        //             {
        //                 primitiveTypeBase.SetValue(
        //                     (IConvertible) Convert.ChangeType(str, conversionType, CultureInfo.InvariantCulture));
        //             }
        //             catch (Exception e)
        //             {
        //                 throw new ValueConversionException($"Failed to parse value [{str}] as type {conversionType}",
        //                     e);
        //             }
        //     }
        //
        //     return primitiveTypeBase;
        // }
        //
        // public static object DoPrimitiveConversion(object value, string str)
        // {
        //     if (value == null)
        //         // we don't know the type, just assume we need a string
        //         return str;
        //
        //     var type = value.GetType();
        //
        //     if (type == typeof(uint))
        //     {
        //         if (str.StartsWith("0x", StringComparison.Ordinal))
        //             return uint.Parse(str.Substring(2), NumberStyles.AllowHexSpecifier);
        //         if (!uint.TryParse(str, out _))
        //             return Vlt32Hasher.Hash(str);
        //     }
        //     else if (type == typeof(int))
        //     {
        //         if (str.StartsWith("0x", StringComparison.Ordinal))
        //             return int.Parse(str.Substring(2), NumberStyles.AllowHexSpecifier);
        //         if (!uint.TryParse(str, out _))
        //             return unchecked((int) Vlt32Hasher.Hash(str));
        //     }
        //
        //     return type.IsEnum ? Enum.Parse(type, str) : Convert.ChangeType(str, type, CultureInfo.InvariantCulture);
        // }
    }
}