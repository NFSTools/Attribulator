using System;

namespace Attribulator.API.Utils;

public static class TypeUtils
{
    public static bool IsPrimitive(Type type)
    {
        // We consider strings and enums to be primitives for convenience.
        return type.IsPrimitive || type.IsEnum || type.IsValueType || type == typeof(string);
    }

    public static bool IsPrimitiveValue(object value)
    {
        return IsPrimitive(value.GetType());
    }
}