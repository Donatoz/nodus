using System.Linq;
using FlowEditor.Models.Templates;
using Nodus.NodeEditor.Models;

namespace FlowEditor.Models.Contexts;

public static class DefaultFlowContexts
{
    public static void Register(INodeContextProvider provider)
    {
        provider.RegisterFactory(DefaultNodes.DebugNodeContextId, () => new DebugContext());
        provider.RegisterFactory(DefaultNodes.ConstantNodeContextId, () => new ConstantContext(
            (_, ctx) => () => ctx.GetMutableProperties().First().PropertyBinding.Getter.Invoke()));
    }
}