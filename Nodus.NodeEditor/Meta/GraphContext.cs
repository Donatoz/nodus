using System.Collections.Generic;
using System.Linq;
using Nodus.Core.Extensions;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Meta;

public readonly record struct GraphContext
{
    public IEnumerable<INodeModel> Nodes { get; }
    public IEnumerable<Connection> Connections { get; }

    public GraphContext(IEnumerable<INodeModel> nodes, IEnumerable<Connection> connections)
    {
        Nodes = nodes;
        Connections = connections;
    }

    public INodeModel? FindNode(string nodeId)
    {
        return Nodes.FirstOrDefault(x => x.NodeId == nodeId);
    }

    public IPortModel? FindPort(string nodeId, string portId)
    {
        return FindNode(nodeId)?.Ports.Value.FirstOrDefault(x => x.Id == portId);
    }

    public INodeModel GetPortOwner(string portId)
    {
        return Nodes.FirstOrDefault(x => x.Ports.Value.Any(y => y.Id == portId))
            .NotNull($"Failed to find port owner for port ({portId})");
    }

    public IEnumerable<Connection> FindPortConnections(string portId)
    {
        return Connections.Where(x => x.SourcePortId == portId || x.TargetPortId == portId);
    }

    public Connection FindPortFirstConnection(string portId)
    {
        return Connections.FirstOrDefault(x => x.SourcePortId == portId || x.TargetPortId == portId);
    }

    public int GetPortConnectionsCount(string portId) => FindPortConnections(portId).Count();
}