using System;
using System.Collections.Generic;
using System.Linq;

namespace Nodus.Core.Extensions;

public static class ReflectionExtensions
{
    public static IEnumerable<Type> GetAsmTypesWithAttribute<T>(this AppDomain domain) where T : Attribute
    {
        return domain.GetAssemblies().SelectMany(asm => asm.GetTypes()
                .Where(x => x.IsDefined(typeof(T), false)));
    }
    
    public static void ForEachAsmTypeWithAttribute<T>(this AppDomain domain, Action<Type> action) where T : Attribute
    {
        ForEachAsmTypeWithAttribute(domain, typeof(T), action);
    }
    
    public static void ForEachAsmTypeWithAttribute(this AppDomain domain, Type attrType, Action<Type> action)
    {
        domain.GetAssemblies()
            .ForEach(asm => asm.GetTypes()
                .Where(x => x.IsDefined(attrType, false))
                .ForEach(action));
    }
}