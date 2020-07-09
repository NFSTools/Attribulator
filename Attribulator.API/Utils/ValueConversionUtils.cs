using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Attribulator.API.Exceptions;
using VaultLib.Core.Hashing;
using VaultLib.Core.Types;
using VaultLib.Core.Types.EA.Reflection;

namespace Attribulator.API.Utils
{
    public static class ValueConversionUtils
    {
        private static readonly Dictionary<Type, Type> TypeCache = new Dictionary<Type, Type>();

        public static VLTBaseType DoPrimitiveConversion(PrimitiveTypeBase primitiveTypeBase, string str)
        {
            var type = primitiveTypeBase.GetType();
            if (TypeCache.TryGetValue(type, out var conversionType))
                return DoPrimitiveConversion(primitiveTypeBase, str, conversionType);

            // Do primitive conversion
            var primitiveInfoAttribute =
                type.GetCustomAttribute<PrimitiveInfoAttribute>();

            if (primitiveInfoAttribute == null)
            {
                // Try to determine enum type
                if (type.IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof(VLTEnumType<>))
                    primitiveInfoAttribute = new PrimitiveInfoAttribute(type.GetGenericArguments()[0]);
                else
                    throw new InvalidDataException("Cannot determine primitive type");
            }

            var primitiveType = primitiveInfoAttribute.PrimitiveType;
            TypeCache[type] = primitiveType;
            return DoPrimitiveConversion(primitiveTypeBase, str, primitiveType);
        }

        private static VLTBaseType DoPrimitiveConversion(PrimitiveTypeBase primitiveTypeBase, string str,
            Type conversionType)
        {
            if (!conversionType.IsEnum)
                try
                {
                    primitiveTypeBase.SetValue(
                        (IConvertible) Convert.ChangeType(str, conversionType, CultureInfo.InvariantCulture));
                }
                catch (Exception e)
                {
                    throw new ValueConversionException($"Failed to parse value [{str}] as type {conversionType}",
                        e);
                }
            else
                primitiveTypeBase.SetValue((IConvertible) Enum.Parse(conversionType, str));

            return primitiveTypeBase;
        }

        public static object DoPrimitiveConversion(object value, string str)
        {
            if (value == null)
                // we don't know the type, just assume we need a string
                return str;

            var type = value.GetType();

            if (type == typeof(uint))
            {
                if (str.StartsWith("0x", StringComparison.Ordinal))
                    return uint.Parse(str.Substring(2), NumberStyles.AllowHexSpecifier);
                if (!uint.TryParse(str, out _))
                    return VLT32Hasher.Hash(str);
            }
            else if (type == typeof(int))
            {
                if (str.StartsWith("0x", StringComparison.Ordinal))
                    return int.Parse(str.Substring(2), NumberStyles.AllowHexSpecifier);
                if (!uint.TryParse(str, out _))
                    return unchecked((int) VLT32Hasher.Hash(str));
            }

            return type.IsEnum ? Enum.Parse(type, str) : Convert.ChangeType(str, type, CultureInfo.InvariantCulture);
        }
    }
}