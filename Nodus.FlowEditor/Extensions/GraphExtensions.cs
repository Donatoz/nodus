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
    public static IFlowNodeContext? TryGetFlowContext(this IFlowNodeModel node)
    {
        return node.Context.Value as IFlowNodeContext;
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

    public static object? GetInputPortValue(this GraphContext context, IFlowPortModel port)
    {
        var connection = context.FindPortFirstConnection(port.Id);

        if (connection.IsValid)
        {
            var sourcePortId = connection.SourcePortId;
            return context.GetFlowPortOwner(sourcePortId).GetPortValue(sourcePortId, context);
        }

        return null;
    }

    public static Connection GetFlowConnection(this GraphContext context, IFlowNodeModel target, PortType targetFlowPortType)
    {
        var port = target.TryGetFlowPort(targetFlowPortType);

        return port == null ? default : context.FindPortFirstConnection(port.Id);
    }
    
    public static IFlowNodeModel? GetFlowPredecessor(this GraphContext context, IFlowNodeModel target)
    {
        var connection = context.GetFlowConnection(target, PortType.Input);
        
        return !connection.IsValid ? null : context.FindNode(connection.SourceNodeId) as IFlowNodeModel;
    }
    
    public static IFlowNodeModel? GetFlowSuccessor(this GraphContext context, IFlowNodeModel target)
    {
        var connection = context.GetFlowConnection(target, PortType.Output);
        
        return !connection.IsValid ? null : context.FindNode(connection.TargetNodeId) as IFlowNodeModel;
    }
}