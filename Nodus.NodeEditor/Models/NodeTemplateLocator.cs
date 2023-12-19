using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Nodus.NodeEditor.Models;

public static class NodeTemplateLocator
{
    public static IEnumerable<NodeTemplate> FetchTemplatesFromAssemblies()
    {
        return AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm =>
        {
            return asm.GetTypes().Where(t => t.IsDefined(typeof(NodeTemplatesContainerAttribute), false))
                .SelectMany(t =>
                {
                    var methods = t.GetMethods().Where(m => m.IsDefined(typeof(NodeTemplateProviderAttribute), false));
                    return methods.Select(m =>
                    {
                        try
                        {
                            var templates = m.Invoke(null, null) as IEnumerable<NodeTemplate>;
                            return templates;
                        }
                        catch (Exception e)
                        {
                            Trace.WriteLine(e.Message);
                            return null;
                        }
                    }).Where(x => x != null).SelectMany(x => x!);
                });
        });
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class NodeTemplatesContainerAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class NodeTemplateProviderAttribute : Attribute
{
}