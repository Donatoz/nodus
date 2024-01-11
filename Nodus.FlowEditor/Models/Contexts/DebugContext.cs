using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FlowEditor.Meta;
using Ninject;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using Serilog;

namespace FlowEditor.Models.Contexts;

public sealed class DebugContext : FlowContextBase
{
    private IFlowPortModel? inPort;
    private ILogger? logger;
    
    public override void Bind(IFlowNodeModel node)
    {
        base.Bind(node);
        
        inPort = node.GetFlowPorts().FirstOrDefault(x => x.Type == PortType.Input && x.ValueType.Value != typeof(FlowType));
    }

    [Inject]
    public void Inject(IFlowLogger logger)
    {
        this.logger = logger;
    }

    protected override void Resolve(IFlow flow, GraphContext context, IFlowToken currentToken)
    {
        if (Node == null || inPort == null || logger == null) return;

        base.Resolve(flow, context, currentToken);
        
        flow.Append(new FlowDelegate(ct =>
        {
            ct.ThrowIfCancellationRequested();

            var msg = Node.GetPortValue(inPort.Id, context);
            logger.Debug(msg?.ToString() ?? "NULL");
            
            return Task.CompletedTask;
        }));
    }
}