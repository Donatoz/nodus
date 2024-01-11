using System.Collections.Generic;
using System.Linq;
using FlowEditor.Models;

namespace FlowEditor;

public static class FlowContextExtensions
{
    public static IEnumerable<FlowContextMutatorProperty> GetMutableProperties(this IFlowContext context)
    {
        return context.Mutator?.GetProperties() ?? Enumerable.Empty<FlowContextMutatorProperty>();
    }
}