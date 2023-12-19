using System;
using Nodus.NodeEditor.Meta;

namespace Nodus.NodeEditor.Models;

public readonly struct NodeTemplate
{
    public NodeData Data { get; }
    public Func<NodeContext?> ContextFactory { get; }

    public static Func<NodeContext?> EmptyContextFactory = () => null;

    public NodeTemplate(NodeData data, Func<NodeContext?>? contextFactory = null)
    {
        ContextFactory = contextFactory ?? EmptyContextFactory;
        Data = data;
    }
}