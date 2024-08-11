using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Nodus.Core.Extensions;
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
        
        ValidateConnectionLoops(connection, context);
        
        if (!sourcePort.IsCompatible(targetPort))
        {
            throw new ArgumentException($"Ports ({sourcePort}) and ({targetPort}) are not compatible");
        }

        ValidatePortCapacity(context, sourcePort);
        ValidatePortCapacity(context, targetPort);
    }

    private static void ValidateConnectionLoops(Connection connection, GraphContext context)
    {
        ValidateConnectionLoopsCore(connection, context, new HashSet<string> {connection.SourceNodeId});
    }

    private static void ValidateConnectionLoopsCore(Connection current, GraphContext context, ISet<string> traversed)
    {
        if (!current.IsValid) return;

        if (traversed.Any(x => x == current.TargetNodeId))
        {
            throw new Exception("Connection results into graph cycle.");
        }

        context.Connections
            .Where(x => x.SourceNodeId == current.TargetNodeId)
            .ForEach(x => ValidateConnectionLoopsCore(x, context, traversed));
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
    
    public static object? GetInputPortValue(this GraphContext context, IPortModel port)
    {
        var connection = context.FindPortFirstConnection(port.Id);

        if (connection.IsValid)
        {
            var sourcePortId = connection.SourcePortId;
            return context.GetPortOwner(sourcePortId)?.GetPortValue(sourcePortId, context);
        }

        return null;
    }
    
    public static IEnumerable<Connection> GetNodeConnections(this GraphContext context, INodeModel node)
    {
        return node.Ports.Value.SelectMany(x => context.FindPortConnections(x.Id));
    }

    public static bool HasAncestor(this INodeModel node, Func<Connection, INodeModel, bool> ancestorValidator, GraphContext context)
    {
        var pendingConnections = new Queue<Connection>();

        context.GetNodeConnections(node).ForEach(pendingConnections.Enqueue);

        while (pendingConnections.Any())
        {
            var connection = pendingConnections.Dequeue();
            var targetNode = context.FindNode(connection.TargetNodeId);

            if (targetNode == null)
            {
                continue;
            }

            if (ancestorValidator.Invoke(connection, targetNode))
            {
                return true;
            }

            context.GetNodeConnections(targetNode)
                .Where(x => x.SourceNodeId == targetNode.NodeId)
                .ForEach(pendingConnections.Enqueue);
        }

        return false;
    }

    public static bool IsAncestorOf(this INodeModel node, INodeModel target, GraphContext context)
    {
        return node.HasAncestor((_, n) => n == target, context);
    }

    public static bool AffectsPort(this INodeModel node, IPortModel port, GraphContext context)
    {
        return node.HasAncestor((c, _) => c.TargetPortId == port.Id, context);
    }
    
    public static IEnumerable<INodeModel> GetOriginNodes(this GraphContext context, INodeModel? relative = null)
    {
        return context.Nodes.Where(node => 
            context.GetNodeConnections(node).All(x => x.SourceNodeId == node.NodeId)
            && (relative == null || node.IsAncestorOf(relative, context)) 
        );
    }

    public static IEnumerable<INodeModel> GetOriginNodes(this GraphContext context, IPortModel target)
    {
        return context.Nodes.Where(node => 
            node.AffectsPort(target, context) && context.GetNodeConnections(node).All(x => x.SourceNodeId == node.NodeId)
        );
    }
    
    private static IEnumerable<INodeModel> GetPredecessorsFrom(this GraphContext context, IEnumerable<INodeModel> originSource, INodeModel node)
    {
        var nodesToVisit = new Queue<INodeModel>();
        var result = new List<INodeModel>();
        var visitedConnections = new HashSet<Connection>(); // The graph context is immutable, hence we need to track the visited connections

        originSource.ForEach(nodesToVisit.Enqueue);

        while (nodesToVisit.Any())
        {
            var current = nodesToVisit.Dequeue();

            if (current == node 
                || !current.IsAncestorOf(node, context)) // The visited node might not lead to the target node
            {
                continue;
            }
            
            result.Add(current);

            foreach (var connection in context.GetNodeConnections(current))
            {
                if (!visitedConnections.Add(connection))
                {
                    continue;
                }

                var next = context.FindNode(connection.TargetNodeId);

                if (next != null && context.GetNodeConnections(next)
                        .Where(x => x.TargetNodeId == next.NodeId)
                        .All(x => visitedConnections.Contains(x)))
                {
                    nodesToVisit.Enqueue(next);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Get the node predecessor nodes in a topological order.
    /// If affected port is specified - the port-affecting predecessors are the only counted.
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<INodeModel> GetSortedPredecessors(this GraphContext context, INodeModel node, IPortModel? affectedPort = null)
    {
        return context.GetPredecessorsFrom(affectedPort != null ? context.GetOriginNodes(affectedPort) : context.GetOriginNodes(node), node);
    }
}