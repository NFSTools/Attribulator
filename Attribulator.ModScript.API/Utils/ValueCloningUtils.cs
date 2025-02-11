using System;
using System.Linq;
using System.Reflection;
using Attribulator.API.Utils;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.DB;
using VaultLib.Core.Types;
using VaultLib.Core.Types.EA.Reflection;
using VaultLib.Core.Utils;

namespace Attribulator.ModScript.API.Utils
{
    /// <summary>
    ///     Exposes a utility function to clone VLT objects
    /// </summary>
    public static class ValueCloningUtils
    {
        /// <summary>
        ///     Creates a complete copy of the given <see cref="VltBaseType" /> object.
        /// </summary>
        /// <param name="originalValue">The object to clone.</param>
        /// <returns>A new instance of the object with all properties copied.</returns>
        public static object CloneValue(object originalValue)
        {
            if (originalValue is IComplexType complexType)
            {
                return complexType.Clone();
            }

            var originalType = originalValue.GetType();
            if (TypeUtils.IsPrimitive(originalType))
            {
                return originalValue;
            }

            throw new Exception($"Cannot clone value of type {originalType}");
        }

        private static object CloneObjectWithReflection<TKey>(Database<TKey> database, object originalValue,
            object newValue,
            VltClass<TKey> vltClass, VltClassField<TKey> vltClassField,
            VltCollection<TKey> vltCollection) where TKey : struct, IKey<TKey>
        {
            // TODO: this needs to handle fields as well. maybe there's an easier method for value types?

            var properties = originalValue.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod?.IsPublic ?? false)
                .ToArray();

            foreach (var propertyInfo in properties)
            {
                var value = propertyInfo.GetValue(originalValue);

                switch (value)
                {
                    case null:
                        propertyInfo.SetValue(newValue, null);
                        continue;
                    case VltBaseType<TKey> vltBaseType:
                        propertyInfo.SetValue(newValue, CloneObjectWithReflection(
                            database,
                            vltBaseType,
                            database.TypeRegistry.ConstructTypeInstance(propertyInfo.PropertyType),
                            vltClass, vltClassField, vltCollection));
                        break;
                    case string str:
                        propertyInfo.SetValue(newValue, new string(str));
                        break;
                    default:
                        if (propertyInfo.PropertyType.IsPrimitive || propertyInfo.PropertyType.IsEnum)
                            propertyInfo.SetValue(newValue, value);
                        else if (value is Array array)
                            propertyInfo.SetValue(newValue, array.Clone());
                        break;
                }
            }

            return newValue;
        }
    }
}