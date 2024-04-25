using System;
using System.Linq;
using DynamicData;
using FlowEditor.Models.Extensions;
using Nodus.Core.Extensions;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace FlowEditor.Models;

public interface IFlowNodeModel : INodeModel
{
    ISourceList<IFlowContextExtension> ContextExtensions { get; }
    
    object? GetPortValue(string portId, GraphContext context);
}

public class FlowNodeModel : NodeModel, IFlowNodeModel
{
    public ISourceList<IFlowContextExtension> ContextExtensions { get; }
    
    private readonly IDisposable contextValueContract;
    
    public FlowNodeModel(string title, NodeTooltip tooltip = default, string? id = null, string? group = null, string? ctxId = null) 
        : base(title, tooltip, id, group, ctxId)
    {
        ContextExtensions = new SourceList<IFlowContextExtension>();
        
        contextValueContract = Context.AlterationStream.Subscribe(OnContextChanged);
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
        if (context is IFlowContext ctx)
        {
            ctx.Bind(this);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing) return;

        this.GetFlowPorts().DisposeAll();
        this.TryGetFlowContext()?.Dispose();
        base.Dispose(disposing);
        
        contextValueContract.Dispose();
        ContextExtensions.Dispose();
    }
}