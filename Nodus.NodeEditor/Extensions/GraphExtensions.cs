using System;
using System.Linq;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Extensions;

public static class GraphExtensions
{
    public static IPortModel? TryFindPort(this INodeModel model, string portId)
    {
        return model.Ports.Value.FirstOrDefault(x => x.Id == portId);
    }
    
    public static void ValidateConnection(this Connection connection, GraphContext context)
    {
        if (context.Connections.Any(x => x.SourceNodeId == connection.SourceNodeId && x.SourcePortId == connection.SourcePortId
                && x.TargetNodeId == connection.TargetNodeId && x.TargetPortId == connection.TargetPortId))
        {
            throw new ArgumentException(
                $"Connection ({connection.SourceNodeId})[{connection.SourcePortId}]->" +
                $"[{connection.TargetPortId}]({connection.TargetNodeId}) was already registered.");
        }

        if (connection.SourcePortId == connection.TargetPortId || connection.SourceNodeId == connection.TargetNodeId)
        {
            throw new ArgumentException("Source and destination nodes/ports must be different");
        }

        var sourcePort = context.Nodes.FirstOrDefault(x => x.NodeId == connection.SourceNodeId)?
            .TryFindPort(connection.SourcePortId);

        if (sourcePort == null)
        {
            throw new NullReferenceException(
                $"Port {connection.SourcePortId} on node {connection.SourceNodeId} was not found");
        }
        
        var targetPort = context.Nodes.FirstOrDefault(x => x.NodeId == connection.TargetNodeId)?
            .TryFindPort(connection.TargetPortId);

        if (targetPort == null)
        {
            throw new NullReferenceException(
                $"Port {connection.TargetPortId} on node {connection.TargetNodeId} was not found");
        }
        
        if (!sourcePort.IsCompatible(targetPort))
        {
            throw new ArgumentException($"Ports ({sourcePort}) and ({targetPort}) are not compatible");
        }

        ValidatePortCapacity(context, sourcePort);
        ValidatePortCapacity(context, targetPort);
    }

    private static void ValidatePortCapacity(GraphContext context, IPortModel port)
    {
        if (port.Capacity != PortCapacity.Multiple && context.GetPortConnectionsCount(port.Id) == 1)
        {
            throw new ArgumentException($"{port.Capacity}-capacity port ({port}) has too many connections.");
        }
    }

    public static void Connect(this ICanvasOperatorModel canvas, Connection connection)
    {
        canvas.Connect(connection.SourceNodeId, connection.SourcePortId, connection.TargetNodeId, connection.TargetPortId);
    }
}