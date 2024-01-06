using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FlowEditor.Models.Templates;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace FlowEditor.Models.Contexts;

public static class DefaultFlowContexts
{
    public static void Register(INodeContextProvider provider)
    {
        provider.RegisterFactory(DefaultNodes.DebugNodeContextId, () => new GenericFlowContext(DebugNodeContext));
        provider.RegisterFactory(DefaultNodes.ConstantNodeContextId, () => new ConstantContext(_ => () => "Hello World"));
    }
    
    private static GenericFlowContext.GenericFlowHandler DebugNodeContext { get; } = (n, g, ct) =>
    {
        ct.ThrowIfCancellationRequested();
        
        var port = n.GetFlowPorts().First(x => x.ValueType.Value == typeof(object));
        var msg = n.GetPortValue(port.Id, g);
        Trace.WriteLine($"DEBUG: {msg}");
        return Task.CompletedTask;
    };
}