using System;
using System.IO;
using System.Linq;
using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.DB;
using VaultLib.Core.Types;
using VaultLib.Core.Types.Attrib;
using VaultLib.Core.Utils;

namespace Attribulator.Plugins.YAMLSupport.Helpers;

internal static class CloakingHelper
{
    public static bool IsTypeAStringInDisguise<TKey>(Type fieldType) where TKey : struct, IKey<TKey>
    {
        return typeof(IStringValue).IsAssignableFrom(fieldType) || typeof(BaseBlob<TKey>).IsAssignableFrom(fieldType);
    }

    private static object UncloakString<TKey>(Database<TKey> database, string sourceDirectory, string str,
        Type realType) where TKey : struct, IKey<TKey>
    {
        var realObject = database.TypeRegistry.ConstructTypeInstance(realType);

        if (realObject is IStringValue stringValue)
        {
            stringValue.SetString(str);
        }
        else if (realObject is BaseBlob<TKey> blob && !string.IsNullOrWhiteSpace(str))
        {
            str = Path.Combine(sourceDirectory, str);
            if (!File.Exists(str))
                throw new InvalidDataException(
                    $"Could not locate blob data file: {str}");
            blob.Data = File.ReadAllBytes(str);
        }

        return realObject;
    }

    public static object UncloakObject<TKey>(Database<TKey> database, string dir,
        VltClassField<TKey> field,
        object serializedValue, Type resolvedType) where TKey : struct, IKey<TKey>
    {
        if (!field.IsArray)
        {
            return UncloakString(database, dir, (string)serializedValue, resolvedType);
        }

        var array = (CustomSerializedArray<string>)serializedValue;

        return new VltArrayType<TKey>(field, resolvedType)
        {
            Items = array.Data.Select(item => UncloakString(database, dir, item, resolvedType))
                .ToList(),
            Capacity = array.Capacity
        };
    }
}