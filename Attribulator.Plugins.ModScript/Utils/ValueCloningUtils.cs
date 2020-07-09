using System;
using System.Linq;
using System.Reflection;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.DB;
using VaultLib.Core.Types;
using VaultLib.Core.Types.EA.Reflection;

namespace Attribulator.Plugins.ModScript.Utils
{
    public static class ValueCloningUtils
    {
        public static VLTBaseType CloneValue(Database database, VLTBaseType originalValue, VltClass vltClass,
            VltClassField vltClassField,
            VltCollection vltCollection)
        {
            var newValue = originalValue is VLTArrayType
                ? TypeRegistry.CreateInstance(database.Options.GameId, vltClass, vltClassField, vltCollection)
                : TypeRegistry.ConstructInstance(
                    TypeRegistry.ResolveType(database.Options.GameId, vltClassField.TypeName), vltClass,
                    vltClassField, vltCollection);

            if (originalValue is VLTArrayType array)
            {
                var newArray = (VLTArrayType) newValue;
                newArray.Capacity = array.Capacity;
                newArray.ItemAlignment = vltClassField.Alignment;
                newArray.FieldSize = vltClassField.Size;
                newArray.Items = array.Items
                    .Select(i => CloneValue(database, i, vltClass, vltClassField, vltCollection)).ToList();

                return newArray;
            }

            switch (originalValue)
            {
                case PrimitiveTypeBase primitiveTypeBase:
                    var convertible = primitiveTypeBase.GetValue();
                    if (convertible != null) ((PrimitiveTypeBase) newValue).SetValue(convertible);
                    return newValue;
                default:
                    return CloneObjectWithReflection(originalValue, newValue, vltClass, vltClassField, vltCollection);
            }
        }

        private static VLTBaseType CloneObjectWithReflection(VLTBaseType originalValue, VLTBaseType newValue,
            VltClass vltClass, VltClassField vltClassField,
            VltCollection vltCollection)
        {
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
                    case VLTBaseType vltBaseType:
                        propertyInfo.SetValue(newValue, CloneObjectWithReflection(
                            vltBaseType,
                            TypeRegistry.ConstructInstance(propertyInfo.PropertyType, vltClass, vltClassField,
                                vltCollection),
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