using Nodus.NodeEditor.Meta;

namespace Nodus.NodeEditor.Models;

public delegate INodeContext? NodeContextFactory();

public readonly struct NodeTemplate : IGraphElementTemplate<NodeData>
{
    public NodeData Data { get; }
    public NodeContextFactory ContextFactory { get; }

    public static readonly NodeContextFactory EmptyContextFactory = () => null;

    public NodeTemplate(NodeData data, NodeContextFactory? contextFactory = null)
    {
        ContextFactory = contextFactory ?? EmptyContextFactory;
        Data = data;
    }

    public NodeTemplate WithContext(NodeContextFactory? contextFactory)
    {
        return new NodeTemplate(Data, contextFactory);
    }
}