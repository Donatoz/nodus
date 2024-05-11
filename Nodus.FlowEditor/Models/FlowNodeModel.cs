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
}

public class FlowNodeModel : NodeModel, IFlowNodeModel
{
    public ISourceList<IFlowContextExtension> ContextExtensions { get; }
    
    public FlowNodeModel(string title, NodeTooltip tooltip = default, string? id = null, string? group = null, string? ctxId = null) 
        : base(title, tooltip, id, group, ctxId)
    {
        ContextExtensions = new SourceList<IFlowContextExtension>();
    }
    
    protected override void Dispose(bool disposing)
    {
        if (!disposing) return;

        this.GetFlowPorts().DisposeAll();
        this.TryGetFlowContext()?.Dispose();
        base.Dispose(disposing);
        
        ContextExtensions.Dispose();
    }
}