using System;
using System.Linq;

namespace Nodus.Core.Extensions;

public static class ReflectionExtensions
{
    public static void ForEachAsmTypeWithAttribute<T>(this AppDomain domain, Action<Type> action) where T : Attribute
    {
        AppDomain.CurrentDomain.GetAssemblies()
            .ForEach(asm => asm.GetTypes()
                .Where(x => x.IsDefined(typeof(T), false))
                .ForEach(action));
    }
}