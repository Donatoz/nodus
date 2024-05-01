using FlowEditor;
using FlowEditor.Meta;
using FlowEditor.Models;
using Nodus.Core.Extensions;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace Nodus.FlowLibraries.Common;

public class ParallelContext : FlowContextBase
{
    private readonly IFlowProducer flowProducer;
    private readonly IGraphFlowBuilder flowBuilder;

    public ParallelContext(IFlowProducer flowProducer, IGraphFlowBuilder flowBuilder)
    {
        this.flowProducer = flowProducer;
        this.flowBuilder = flowBuilder;
    }
    
    protected override void AlterFlow(IFlow flow, GraphContext context, IFlowToken currentToken)
    {
        if (Node == null) return;
        
        // Get the branch flow port (typically the last output one) connection
        
        var outputFlowPorts = Node.GetFlowPorts()
            .Where(x => x.ValueType.Value == typeof(FlowType) && x.Type == PortType.Output);

        if (outputFlowPorts.Count() < 2)
        {
            throw new Exception("A branch context must have at least 2 unique output flow ports.");
        }
        
        var branchConnection = context.FindPortFirstConnection(outputFlowPorts.Last().Id);
        
        if (!branchConnection.IsValid) return;
        
        // Build the flow from the root which is the first node connected to the parallel flow port
        
        var token = flowBuilder.GetRootToken(context, context.FindNode(branchConnection.TargetNodeId).MustBe<IFlowNodeModel>(), branchConnection);
        var rootBranchUnit = flowProducer.BuildFlow(token);
        
        flow.Append(new FlowDelegate("Parallel Context", ct => GetContext(rootBranchUnit, ct)));
    }

    private Task GetContext(IFlowUnit rootUnit, CancellationToken ct)
    {
        Task.Run(() => rootUnit.Execute(ct), ct);

        return Task.CompletedTask;
    }
}