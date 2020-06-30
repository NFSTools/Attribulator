using System;
using System.Linq;
using System.Reflection;
using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.DB;
using VaultLib.Core.Types;
using VaultLib.Core.Types.EA.Reflection;

namespace YAMLDatabase.ModScript.Utils
{
    public static class ValueCloningUtils
    {
        public static VLTBaseType CloneValue(Database database, VLTBaseType originalValue, VltClass vltClass, VltClassField vltClassField,
            VltCollection vltCollection)
        {

            var newValue = (originalValue is VLTArrayType)
                ? TypeRegistry.CreateInstance(database.Options.GameId, vltClass, vltClassField, vltCollection)
                : TypeRegistry.ConstructInstance(TypeRegistry.ResolveType(database.Options.GameId, vltClassField.TypeName), vltClass,
                    vltClassField, vltCollection);

            if (originalValue is VLTArrayType array)
            {
                var newArray = (VLTArrayType)newValue;
                newArray.Capacity = array.Capacity;
                newArray.ItemAlignment = vltClassField.Alignment;
                newArray.FieldSize = vltClassField.Size;
                newArray.Items = array.Items.Select(i => CloneValue(database, i, vltClass, vltClassField, vltCollection)).ToList();

                return newArray;
            }

            switch (originalValue)
            {
                case PrimitiveTypeBase primitiveTypeBase:
                    var convertible = primitiveTypeBase.GetValue();
                    if (convertible != null)
                    {
                        ((PrimitiveTypeBase)newValue).SetValue(convertible);
                    }
                    return newValue;
                default:
                    return CloneObjectWithReflection(originalValue, newValue, vltClass, vltClassField, vltCollection);
            }
        }

        private static VLTBaseType CloneObjectWithReflection(VLTBaseType originalValue, VLTBaseType newValue, VltClass vltClass, VltClassField vltClassField,
            VltCollection vltCollection)
        {
            PropertyInfo[] properties = originalValue.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.SetMethod?.IsPublic ?? false)
                .ToArray();

            foreach (var propertyInfo in properties)
            {
                if (propertyInfo.PropertyType.IsSubclassOf(typeof(VLTBaseType)))
                {
                    propertyInfo.SetValue(newValue, CloneObjectWithReflection(
                        propertyInfo.GetValue(originalValue) as VLTBaseType,
                        Activator.CreateInstance(propertyInfo.PropertyType, vltClass, vltClassField, vltCollection) as
                            VLTBaseType,
                        vltClass, vltClassField, vltCollection));
                }
                else if (propertyInfo.PropertyType == typeof(string))
                {
                    propertyInfo.SetValue(newValue, new string(propertyInfo.GetValue(originalValue) as string));
                }
                else if (propertyInfo.PropertyType.IsPrimitive || propertyInfo.PropertyType.IsEnum)
                {
                    propertyInfo.SetValue(newValue, propertyInfo.GetValue(originalValue));
                }
                else if (propertyInfo.PropertyType.IsArray && propertyInfo.GetValue(originalValue) != null)
                {
                    propertyInfo.SetValue(newValue, ((Array)propertyInfo.GetValue(originalValue)).Clone());
                }
            }

            return newValue;
        }
    }
}