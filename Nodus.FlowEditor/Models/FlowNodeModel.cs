using System;
using System.Linq;
using System.Reactive;
using Nodus.Core.Extensions;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace FlowEditor.Models;

public interface IFlowNodeModel : INodeModel
{
    object? GetPortValue(string portId, GraphContext context);
}

public class FlowNodeModel : NodeModel, IFlowNodeModel
{
    private readonly IDisposable contextValueContract;
    
    public FlowNodeModel(string title, NodeTooltip tooltip = default, string? id = null, string? group = null, string? ctxId = null) 
        : base(title, tooltip, id, group, ctxId)
    {
        contextValueContract = Context.AlterationStream.Subscribe(Observer.Create<INodeContext?>(OnContextChanged));
    }

    public object? GetPortValue(string portId, GraphContext context)
    {
        var port = this.GetFlowPorts().FirstOrDefault(x => x.Id == portId);

        if (port == null)
        {
            throw new ArgumentException($"Failed to get port value ({portId}): port not found.");
        }

        return this.TryGetFlowContext()?.ResolvePortValue(port, context);
    }

    private void OnContextChanged(INodeContext? context)
    {
        if (context is IFlowNodeContext ctx)
        {
            ctx.Bind(this);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing) return;

        this.GetFlowPorts().DisposeAll();
        base.Dispose(disposing);
        
        contextValueContract.Dispose();
        this.TryGetFlowContext()?.Dispose();
    }
}