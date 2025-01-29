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
        /// <param name="database">The database to resolve types from.</param>
        /// <param name="originalValue">The object to clone.</param>
        /// <param name="vltClass">The VLT class holding the field.</param>
        /// <param name="vltClassField">The VLT field holding the object.</param>
        /// <param name="vltCollection">The VLT collection.</param>
        /// <returns>A new instance of the object with all properties copied.</returns>
        public static object CloneValue<TKey>(Database<TKey> database, object originalValue, VltClass<TKey> vltClass,
            VltClassField<TKey> vltClassField,
            VltCollection<TKey> vltCollection) where TKey : struct, IKey<TKey>
        {
            var originalType = originalValue.GetType();
            if (TypeUtils.IsPrimitive(originalType))
            {
                return originalValue;
            }

            if (originalValue is VltArrayType<TKey> array)
            {
                var newArray = (VltArrayType<TKey>)FieldUtils.CreateFieldValue(database.TypeRegistry, vltClassField);
                newArray.Capacity = array.Capacity;
                newArray.Items = array.Items
                    .Select(i => CloneValue(database, i, vltClass, vltClassField, vltCollection)).ToList();

                return newArray;
            }


            var newValue = FieldUtils.ConstructFieldType(database.TypeRegistry, vltClassField);

            switch (originalValue)
            {
                case IStringValue stringValue:
                    var str = stringValue.GetString();
                    ((IStringValue)newValue).SetString(str);
                    return newValue;
                default:
                    return CloneObjectWithReflection(database, originalValue, newValue, vltClass, vltClassField,
                        vltCollection);
            }
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