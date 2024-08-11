using System.Collections;
using System.Diagnostics;

namespace Nodus.ObjectDescriptor;

public static class ObjectDescriptionUtility
{
    public static bool IsPrimitive(Type type)
    {
        return type.IsPrimitive || type.IsPointer || type.IsEnum || type == typeof(string);
    }

    public static bool IsCollection(Type type)
    {
        return type == typeof(IList) || type.GetInterfaces().Any(x => x == typeof(IList));
    }
}