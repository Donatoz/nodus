using System;
using System.Collections.Generic;
using System.Linq;
using Nodus.NodeEditor.Extensions;
using Nodus.NodeEditor.Meta;

namespace Nodus.NodeEditor.Models;

public interface INodeContext
{
    void Deserialize(NodeContextData data);
    NodeContextData? Serialize();
    void Bind(INodeModel node);

    object? ResolvePortValue(IPortModel port, GraphContext context);
}

public abstract class NodeContextBase<TNode> : INodeContext where TNode : INodeModel
{
    public abstract void Deserialize(NodeContextData data);
    public abstract NodeContextData? Serialize();
    
    protected TNode? Node { get; private set; }
    
    private readonly IDictionary<IPortModel, Func<GraphContext, object?>> portValueBindings;

    protected NodeContextBase()
    {
        portValueBindings = new Dictionary<IPortModel, Func<GraphContext, object?>>();
    }
    
    public virtual void Bind(INodeModel node)
    {
        if (node is not TNode n)
        {
            throw new ArgumentException($"Failed to bind node ({node}) to {this}: node must be of type: {typeof(TNode)}.");
        }
        
        Node = n;
        ResetPortBindings();
    }
    
    public object? ResolvePortValue(IPortModel port, GraphContext context)
    {
        return port.Type == PortType.Input ? context.GetInputPortValue(port) : GetOutputValue(port, context);
    }

    protected void BindPortValue(IPortModel port, Func<GraphContext, object?> valueGetter)
    {
        portValueBindings[port] = valueGetter;
    }
    
    protected void TryBindFirstOutPort(Func<GraphContext, object?> valueGetter) => TryBindOutPort(0, valueGetter);

    protected void TryBindOutPort(int index, Func<GraphContext, object?> valueGetter)
    {
        var port = Node?.Ports.Value.Where(x => x.Type == PortType.Output).ElementAt(index);
        
        if (port == null) return;
        
        BindPortValue(port, valueGetter);
    }

    protected void ResetPortBindings()
    {
        portValueBindings.Clear();
    }
    
    protected object? GetOutputValue(IPortModel port, GraphContext context)
    {
        if (!portValueBindings.TryGetValue(port, out var binding))
        {
            throw new Exception($"Port ({port.Header}) value is not bound to anything.");
        }

        return binding.Invoke(context);
    }
}