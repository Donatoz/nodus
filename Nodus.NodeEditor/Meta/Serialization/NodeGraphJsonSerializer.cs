using System;
using Newtonsoft.Json;

namespace Nodus.NodeEditor.Meta.Serialization;

public class NodeGraphJsonSerializer : INodeGraphSerializer
{
    public static NodeGraphJsonSerializer Default { get; } = new();

    private readonly JsonSerializerSettings serializerSettings;

    public NodeGraphJsonSerializer(JsonSerializerSettings? settings = null)
    {
        serializerSettings = settings ?? new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };
    }
    
    public object Serialize(NodeGraph graph)
    {
        return JsonConvert.SerializeObject(graph, Formatting.Indented, serializerSettings);
    }

    public NodeGraph? Deserialize(object data)
    {
        if (data is not string s)
        {
            throw new ArgumentException($"Data must be a json string: {data}");
        }
        
        return JsonConvert.DeserializeObject<NodeGraph>(s, serializerSettings);
    }
}