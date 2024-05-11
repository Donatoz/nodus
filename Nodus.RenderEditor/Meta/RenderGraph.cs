using System.Collections.Generic;
using Newtonsoft.Json;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace Nodus.RenderEditor.Meta;

public record RenderGraph : NodeGraph
{
    public RenderGraphVariable[] GraphVariables { get; } 
    
    [JsonConstructor]
    public RenderGraph(string graphName, IEnumerable<IGraphElementData> elements, IEnumerable<Connection> connections, RenderGraphVariable[] graphVariables) 
        : base(graphName, elements, connections)
    {
        GraphVariables = graphVariables;
    }

    public RenderGraph(NodeGraph other, RenderGraphVariable[] variables) : this(other.GraphName, other.Elements, other.Connections, variables)
    {
    }
}