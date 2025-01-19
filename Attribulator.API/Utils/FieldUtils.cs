using VaultLib.Core;
using VaultLib.Core.Data;
using VaultLib.Core.Types;

namespace Attribulator.API.Utils;

public static class FieldUtils
{
    public static object CreateFieldValue(TypeRegistry typeRegistry, VltClassField field)
    {
        var resolvedType = typeRegistry.ResolveType(field.TypeName);
        return field.IsArray
            ? new VltArrayType(field, resolvedType)
            : typeRegistry.ConstructTypeInstance(resolvedType, field);
    }

    public static object ConstructFieldType(TypeRegistry typeRegistry, VltClassField field)
    {
        return typeRegistry.ConstructTypeInstance(typeRegistry.ResolveType(field.TypeName), field);
    }
}