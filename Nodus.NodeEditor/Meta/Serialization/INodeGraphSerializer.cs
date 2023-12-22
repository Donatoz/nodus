using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Meta.Serialization;

public interface INodeGraphSerializer
{
    object Serialize(NodeGraph graph);
    NodeGraph? Deserialize(object data);
}