using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using VaultLib.Core.Hashing;
using VaultLib.Core.Types;
using VaultLib.Core.Types.EA.Reflection;

namespace YAMLDatabase.ModScript.Utils
{
    public static class ValueConversionUtils
    {
        public static VLTBaseType DoPrimitiveConversion(PrimitiveTypeBase primitiveTypeBase, string str)
        {
            // Do primitive conversion
            var primitiveInfoAttribute =
                primitiveTypeBase.GetType().GetCustomAttribute<PrimitiveInfoAttribute>();

            if (primitiveInfoAttribute == null)
            {
                // Try to determine enum type
                if (primitiveTypeBase.GetType().IsGenericType &&
                    primitiveTypeBase.GetType().GetGenericTypeDefinition() == typeof(VLTEnumType<>))
                {
                    primitiveInfoAttribute = new PrimitiveInfoAttribute(primitiveTypeBase.GetType().GetGenericArguments()[0]);
                }
                else
                {
                    throw new InvalidDataException("Cannot determine primitive type");
                }
            }

            if (primitiveInfoAttribute.PrimitiveType.IsEnum)
            {
                if (str.StartsWith("0x") &&
                    uint.TryParse(str.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint val))
                {
                    primitiveTypeBase.SetValue((IConvertible)Enum.Parse(primitiveInfoAttribute.PrimitiveType, val.ToString()));
                }
                else
                {
                    primitiveTypeBase.SetValue((IConvertible)Enum.Parse(primitiveInfoAttribute.PrimitiveType, str));
                }
            }
            else
            {
                if (str.StartsWith("0x") && uint.TryParse(str.Substring(2), NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture, out uint val))
                {
                    primitiveTypeBase.SetValue((IConvertible)Convert.ChangeType(val, primitiveInfoAttribute.PrimitiveType));
                }
                else
                {
                    primitiveTypeBase.SetValue(
                        (IConvertible)Convert.ChangeType(str, primitiveInfoAttribute.PrimitiveType, CultureInfo.InvariantCulture));
                }
            }

            return primitiveTypeBase;
        }

        public static object DoPrimitiveConversion(object value, string str)
        {
            if (value == null)
            {
                // we don't know the type, just assume we need a string
                return str;
            }

            Type type = value.GetType();

            if (type == typeof(uint))
            {
                if (str.StartsWith("0x"))
                    return uint.Parse(str.Substring(2), NumberStyles.AllowHexSpecifier);
                if (!uint.TryParse(str, out _))
                    return VLT32Hasher.Hash(str);
            }
            else if (type == typeof(int))
            {
                if (str.StartsWith("0x"))
                    return int.Parse(str.Substring(2), NumberStyles.AllowHexSpecifier);
                if (!uint.TryParse(str, out _))
                    return unchecked((int)VLT32Hasher.Hash(str));
            }

            return type.IsEnum ? Enum.Parse(type, str) : Convert.ChangeType(str, type, CultureInfo.InvariantCulture);
        }
    }
}