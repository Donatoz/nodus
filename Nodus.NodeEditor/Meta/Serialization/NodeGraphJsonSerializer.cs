using System;
using Newtonsoft.Json;

namespace Nodus.NodeEditor.Meta.Serialization;

public class NodeGraphJsonSerializer : INodeGraphSerializer
{
    public static NodeGraphJsonSerializer Default { get; } = new();
    
    public object Serialize(NodeGraph graph)
    {
        return JsonConvert.SerializeObject(graph, Formatting.Indented);
    }

    public NodeGraph? Deserialize(object data)
    {
        if (data is not string s)
        {
            throw new ArgumentException($"Data must be a json string: {data}");
        }
        
        return JsonConvert.DeserializeObject<NodeGraph>(s);
    }
}