using System.Collections.Generic;
using System.Linq;
using FlowEditor.Meta;
using FlowEditor.Models;
using Nodus.Core.Extensions;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace FlowEditor;

public static class GraphExtensions
{
    public static IFlowContext? TryGetFlowContext(this IFlowNodeModel node)
    {
        return node.Context.Value as IFlowContext;
    }
    
    public static IFlowPortModel? TryGetFlowPort(this IFlowNodeModel node, PortType type)
    {
        return node.Ports.Value.FirstOrDefault(x => x is IFlowPortModel p 
                                                    && p.Type == type 
                                                    && p.ValueType.Value == typeof(FlowType)) as IFlowPortModel;
    }

    public static IEnumerable<IFlowPortModel> GetFlowPorts(this IFlowNodeModel node)
    {
        return node.Ports.Value.Cast<IFlowPortModel>();
    } 

    public static IFlowNodeModel GetFlowPortOwner(this GraphContext context, string portId)
    {
        return context.GetPortOwner(portId)
            .MustBe<IFlowNodeModel>($"Failed to get flow port ({portId}) owner: owner is not flow node.");
    }

    public static IFlowPortModel? GetFlowSuccessionPort(this IFlowNodeModel node, GraphContext ctx)
    {
        if (node.Context.Value is IFlowContext fc && fc.GetEffectiveSuccessionPort(ctx) is { } p)
        {
            return p;
        }
        
        return node.Ports.Value.FirstOrDefault(x => x is IFlowPortModel { Type: PortType.Output } p
                                                    && p.ValueType.Value == typeof(FlowType)) as IFlowPortModel;
    }
    
    public static (IFlowNodeModel?, Connection?) GetFlowSuccessor(this GraphContext context, IFlowNodeModel target)
    {
        var port = target.GetFlowSuccessionPort(context);
        
        if (port == null) return default;
        
        var connection = context.FindPortFirstConnection(port.Id);
        
        return !connection.IsValid ? default : (context.FindNode(connection.TargetNodeId) as IFlowNodeModel, connection);
    }

    public static (IFlowNodeModel, Connection)[] GetSuccessionCandidates(this IFlowNodeModel node, GraphContext context)
    {
        var ports = node.GetFlowPorts().Where(x => x.Type == PortType.Output && x.ValueType.Value == typeof(FlowType));
        return ports
            .Select(x => context.FindPortFirstConnection(x.Id))
            .Where(x => x.IsValid)
            .Select(x => (context.FindNode(x.TargetNodeId).MustBe<IFlowNodeModel>(), x))
            .ToArray();
    }
}