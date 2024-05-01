using System;
using System.Collections.Generic;
using System.Linq;

namespace FlowEditor.Models.Primitives;

public enum RuntimeType
{
    String,
    Number,
    Boolean
}

public static class RuntimeTypeExtensions
{
    private static readonly IDictionary<Type, RuntimeType> suportedTypes = new Dictionary<Type, RuntimeType>
    {
        {typeof(string), RuntimeType.String},
        {typeof(float), RuntimeType.Number},
        {typeof(int), RuntimeType.Number},
        {typeof(double), RuntimeType.Number},
        {typeof(bool), RuntimeType.Boolean}
    };
    
    public static Type ToClrType(this RuntimeType type)
    {
        return suportedTypes.FirstOrDefault(x => x.Value == type).Key ?? typeof(object);
    }

    public static RuntimeType ToRuntimeType(this Type type)
    {
        return suportedTypes.TryGetValue(type, out var t) ? t : RuntimeType.Number;
    }

    public static object GetDefaultValue(this RuntimeType type)
    {
        return type switch
        {
            RuntimeType.Number => 0f,
            RuntimeType.String => string.Empty,
            RuntimeType.Boolean => false,
            _ => 0f
        };
    }
}