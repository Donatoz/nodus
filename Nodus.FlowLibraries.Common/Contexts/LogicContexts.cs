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

    public override void Bind(IFlowNodeModel node)
    {
        base.Bind(node);

        var inputPorts = node.GetFlowPorts().Where(x => x.Type == PortType.Input);

        if (inputPorts.Count() < 2)
        {
            throw new Exception("A compare context must have at least 2 unique input ports.");
        }
        
        TryBindFirstOutPort(ctx => GetExposedValue<CompareOperation>(OperationName)
            .Evaluate(node.GetPortValue(inputPorts.First().Id, ctx).MustBeNumber(),
                node.GetPortValue(inputPorts.Last().Id, ctx).MustBeNumber()));
    }
}