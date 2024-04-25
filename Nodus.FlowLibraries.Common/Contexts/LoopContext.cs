using System.ComponentModel.DataAnnotations;
using FlowEditor;
using FlowEditor.Meta;
using FlowEditor.Models;
using FlowEditor.Models.Contexts;
using Ninject;
using Nodus.Core.Extensions;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace Nodus.FlowLibraries.Common;

public class LoopContext : CachedExposeContext
{
    private const string IterationsCountName = "IterationsCount";

    private readonly IGraphFlowBuilder flowBuilder;
    
    public LoopContext(IGraphFlowBuilder flowBuilder)
    {
        this.flowBuilder = flowBuilder;
        ExposeValue(IterationsCountName, "Default Iterations", 1, true, new RangeAttribute(1, 99));
    }

    protected override IFlowToken GetEffectiveFlowToken(IFlowToken original, GraphContext context)
    {
        if (Node == null) return original;

        // Get the loop flow port (typically the last output one) connection
        
        var outputFlowPorts = Node.GetFlowPorts().Where(x => x.ValueType.Value == typeof(FlowType) && x.Type == PortType.Output);

        if (outputFlowPorts.Count() < 2)
        {
            throw new Exception("A loop context must have at least 2 unique output flow ports.");
        }

        var iterPort = Node.GetFlowPorts().LastOrDefault(x => x.Type == PortType.Input && x.ValueType.Value != typeof(FlowType));

        if (iterPort == null)
        {
            throw new Exception("A loop context must a numeric input port.");
        }
        
        var loopConnection = context.FindPortFirstConnection(outputFlowPorts.Last().Id);

        if (!loopConnection.IsValid) return original;
        
        // Build the flow from the first inner loop node
        
        var token = flowBuilder.GetRootToken(context, context.FindNode(loopConnection.TargetNodeId).MustBe<IFlowNodeModel>(), loopConnection);
        var iterations = Convert.ToInt32(Node.GetPortValue(iterPort.Id, context)?.MustBeNumber() ?? GetExposedValue<int>(IterationsCountName));
        
        // Add it as a child specified amount of times.

        for (var i = 0; i < iterations; i++)
        {
            original.AddChild(token);
        }
        
        return original;
    }
}