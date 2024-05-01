using System.Collections.Generic;
using System.Linq;
using Nodus.Core.Extensions;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Meta;

/// <summary>
/// Represents a snapshot of the node graph state.
/// </summary>
/// <remarks>
/// This context is performance critical, as it serves as a node graph immutable representation and is
/// used in all scenarios where graph node or connection data is needed.
/// <para>
/// This context allocates new memory upon construction, meaning that it is discouraged to create new graph
/// contexts each time.
/// </para>
/// </remarks>
public record GraphContext
{
    public IEnumerable<INodeModel> Nodes { get; }
    public IEnumerable<Connection> Connections { get; }

    // TODO: Maybe these caches should be dynamic and be populated by
    // the canvas instead of being an init-only state snapshot.
    private readonly IDictionary<string, INodeModel> nodeCache;
    private readonly IDictionary<string, Connection[]> portConnectionsCache;
    private readonly IDictionary<string, string> portOwners;
    
    public GraphContext(IEnumerable<INodeModel> nodes, IEnumerable<Connection> connections)
    {
        Nodes = nodes;
        Connections = connections;

        nodeCache = new Dictionary<string, INodeModel>();
        portConnectionsCache = new Dictionary<string, Connection[]>();
        portOwners = new Dictionary<string, string>();
        
        foreach (var node in Nodes)
        {
            nodeCache[node.NodeId] = node;

            foreach (var port in node.Ports.Value)
            {
                portConnectionsCache[port.Id] =
                    Connections.Where(x => x.SourcePortId == port.Id || x.TargetPortId == port.Id).ToArray();
                portOwners[port.Id] = node.NodeId;
            }
        }
    }

    public INodeModel? FindNode(string nodeId)
    {
        return nodeCache.TryGetValue(nodeId, out var value) ? value : null;
    }

    public IPortModel? FindPort(string nodeId, string portId)
    {
        return FindNode(nodeId)?.Ports.Value.FirstOrDefault(x => x.Id == portId);
    }

    public INodeModel? GetPortOwner(string portId)
    {
        return portOwners.TryGetValue(portId, out var owner) ? FindNode(owner) : null;
    }

    public IEnumerable<Connection> FindPortConnections(string portId)
    {
        return portConnectionsCache.TryGetValue(portId, out var c) ? c : Enumerable.Empty<Connection>();
    }

    public Connection FindPortFirstConnection(string portId)
    {
        return FindPortConnections(portId).FirstOrDefault();
    }

    public int GetPortConnectionsCount(string portId) => FindPortConnections(portId).Count();
}