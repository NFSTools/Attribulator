using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.DataInterfaces;
using VaultLib.Core.Types;

namespace Attribulator.API.Utils;

public static class FieldUtils
{
    public static object CreateFieldValue<TKey>(TypeRegistry<TKey> typeRegistry, VltClassField<TKey> field)
        where TKey : struct, IKey<TKey>
    {
        var resolvedType = typeRegistry.ResolveFieldType(field);
        return field.IsArray
            ? new VltArrayType<TKey>(field, resolvedType)
            : typeRegistry.ConstructTypeInstance(resolvedType);
    }

    public static object ConstructFieldType<TKey>(TypeRegistry<TKey> typeRegistry, VltClassField<TKey> field)
        where TKey : struct, IKey<TKey>
    {
        return typeRegistry.ConstructTypeInstance(typeRegistry.ResolveFieldType(field));
    }
}