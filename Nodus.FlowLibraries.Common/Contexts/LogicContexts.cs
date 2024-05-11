using System.Diagnostics;
using FlowEditor;
using FlowEditor.Models;
using FlowEditor.Models.Contexts;
using FlowEditor.Models.Primitives;
using Nodus.Core.Extensions;
using Nodus.NodeEditor.Models;

namespace Nodus.FlowLibraries.Common;

public class CompareContext : CachedExposeContext
{
    private const string OperationName = "Operation";
    
    public CompareContext()
    {
        ExposeValue(OperationName, "Operation", CompareOperation.Equal);
    }

    public override void Bind(INodeModel node)
    {
        base.Bind(node);

        var inputPorts = Node!.GetFlowPorts().Where(x => x.Type == PortType.Input);

        if (inputPorts.Count() < 2)
        {
            throw new Exception("A compare context must have at least 2 unique input ports.");
        }
        
        TryBindFirstOutPort(ctx => GetExposedValue<CompareOperation>(OperationName)
            .Evaluate(Node!.GetPortValue(inputPorts.First().Id, ctx).MustBeNumber(),
                Node!.GetPortValue(inputPorts.Last().Id, ctx).MustBeNumber()));
    }
}

public class LogicContext : CachedExposeContext
{
    private const string OperationName = "Operation";
    private const string DefaultAName = "A";
    private const string DefaultBName = "B";

    public LogicContext()
    {
        ExposeValue(OperationName, "Operation", LogicalOperation.And);
        ExposeValue(DefaultAName, "Default A", false);
        ExposeValue(DefaultBName, "Default B", false);
    }

    public override void Bind(INodeModel node)
    {
        base.Bind(node);
        
        var inputPorts = Node!.GetFlowPorts().Where(x => x.Type == PortType.Input);

        if (inputPorts.Count() < 2)
        {
            throw new Exception("A logic context must have at least 2 unique input ports.");
        }
        
        TryBindFirstOutPort(ctx => GetExposedValue<LogicalOperation>(OperationName)
            .Evaluate(Node!.GetPortValue(inputPorts.First().Id, ctx) as bool? ?? GetExposedValue<bool>(DefaultAName),
                Node!.GetPortValue(inputPorts.Last().Id, ctx) as bool? ?? GetExposedValue<bool>(DefaultBName)));
    }
}