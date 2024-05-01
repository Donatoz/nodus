using System.Collections.Generic;
using System.Linq;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Meta;

public record NodeGraph
{
    public string GraphName { get; init; }
    public IGraphElementData[] Elements { get; init; }
    public Connection[] Connections { get; init; }
    
    public NodeGraph(string graphName, IEnumerable<IGraphElementData> elements, IEnumerable<Connection> connections)
    {
        GraphName = graphName;
        Elements = elements.ToArray();
        Connections = connections.ToArray();
    }

    public static NodeGraph CreateEmpty()
    {
        return new NodeGraph("New Graph", Enumerable.Empty<IGraphElementData>(), Enumerable.Empty<Connection>());
    }
}