using System.Collections.Generic;
using System.Linq;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Meta;

public record NodeGraph
{
    public string GraphName { get; init; }
    public NodeData[] Nodes { get; init; }
    public Connection[] Connections { get; init; }
    
    public NodeGraph(string graphName, IEnumerable<NodeData> nodes, IEnumerable<Connection> connections)
    {
        GraphName = graphName;
        Nodes = nodes.ToArray();
        Connections = connections.ToArray();
    }

    public static NodeGraph CreateEmpty()
    {
        return new NodeGraph("New Graph", Enumerable.Empty<NodeData>(), Enumerable.Empty<Connection>());
    }
}