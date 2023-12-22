using System;
using Nodus.NodeEditor.Meta;

namespace Nodus.NodeEditor.Models;

public readonly struct NodeTemplate
{
    public NodeData Data { get; }
    public Func<INodeContext?> ContextFactory { get; }

    public static Func<INodeContext?> EmptyContextFactory = () => null;

    public NodeTemplate(NodeData data, Func<INodeContext?>? contextFactory = null)
    {
        ContextFactory = contextFactory ?? EmptyContextFactory;
        Data = data;
    }
}