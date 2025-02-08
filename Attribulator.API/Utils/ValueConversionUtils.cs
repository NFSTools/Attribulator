using System;
using System.Globalization;
using VaultLib.Core.DataInterfaces;

namespace Attribulator.API.Utils
{
    public static class ValueConversionUtils
    {
        public static object ConvertPrimitiveToNewPrimitive(Type primitiveType, string primitiveString)
        {
            if (primitiveType == typeof(string))
                return primitiveString;

            if (primitiveType.IsEnum)
            {
                // 3 acceptable input formats:
                // 1. Name (string)
                // 2. Value (decimal)
                // 3. Value (0x<hex>) (this may disappear eventually?)

                if (!primitiveString.StartsWith("0x")) return Enum.Parse(primitiveType, primitiveString);

                if (!uint.TryParse(primitiveString.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                        out var result))
                {
                    throw new FormatException(
                        $"Can't interpret {primitiveString} as a hexadecimal value for conversion to enum {primitiveType.Name}.");
                }

                return Enum.ToObject(primitiveType, result);
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

            if (primitiveType == typeof(BinKey32))
            {
                return KeyUtils.StringToKey<BinKey32>(primitiveString);
            }

            if (primitiveType == typeof(BinKey64))
            {
                return KeyUtils.StringToKey<BinKey64>(primitiveString);
            }

            if (primitiveType.IsPrimitive)
            {
                return primitiveString.StartsWith("0x")
                    ? ConvertHexToPrimitive(primitiveType, primitiveString[2..])
                    : Convert.ChangeType(primitiveString, primitiveType, CultureInfo.InvariantCulture);
            }

            throw new InvalidCastException($"Can't convert input string '{primitiveString}' to {primitiveType}.");
        }

        private static object ConvertHexToPrimitive(Type primitiveType, string hexString)
        {
            if (primitiveType == typeof(ulong))
                return ulong.Parse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            if (primitiveType == typeof(long))
                return long.Parse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            if (primitiveType == typeof(uint))
                return uint.Parse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            if (primitiveType == typeof(int))
                return int.Parse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            if (primitiveType == typeof(ushort))
                return ushort.Parse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            if (primitiveType == typeof(short))
                return short.Parse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            if (primitiveType == typeof(byte))
                return byte.Parse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            if (primitiveType == typeof(sbyte))
                return sbyte.Parse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            throw new InvalidCastException($"Can't convert hexadecimal string '{hexString}' to {primitiveType}.");
        }
    }
}