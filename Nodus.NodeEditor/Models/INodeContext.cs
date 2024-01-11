using Nodus.NodeEditor.Meta;

namespace Nodus.NodeEditor.Models;

public interface INodeContext
{
    void Deserialize(NodeContextData data);
    NodeContextData? Serialize();
}