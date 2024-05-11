using FlowEditor;
using FlowEditor.Meta;
using FlowEditor.Models;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace Nodus.FlowLibraries.Common;

public sealed class DebugContext : FlowContextBase
{
    private IFlowPortModel? inPort;
    
    private readonly IFlowLogger logger;

    public DebugContext(IFlowLogger logger)
    {
        this.logger = logger;
    }
    
    public override void Bind(INodeModel node)
    {
        base.Bind(node);
        
        inPort = Node!.GetFlowPorts().FirstOrDefault(x => x.Type == PortType.Input && x.ValueType.Value != typeof(FlowType));
    }

    protected override Task Resolve(GraphContext context, IFlowToken currentToken, CancellationToken ct)
    {
        if (Node == null || inPort == null) return Task.CompletedTask;
        
        ct.ThrowIfCancellationRequested();

        var msg = Node.GetPortValue(inPort.Id, context);
        logger.Debug(msg?.ToString() ?? "Null");
            
        return Task.CompletedTask;
    }
}