using System;
using System.Collections.Generic;
using System.Linq;
using Nodus.Core.Reactive;

namespace Nodus.NodeEditor.Models;

public interface INodeModel : IDisposable
{
    string NodeId { get; }
    string Title { get; }
    string? Group { get; }
    
    IReactiveProperty<NodeTooltip> Tooltip { get; }
    IReactiveProperty<IEnumerable<IPortModel>> Ports { get; }
    IReactiveProperty<NodeContext?> Context { get; }

    void AddPort(IPortModel port);
    void ChangeContext(NodeContext? context);
}

public class NodeModel : INodeModel
{
    public string NodeId { get; }
    public string Title { get; }
    public string? Group { get; }

    private readonly MutableReactiveProperty<List<IPortModel>> ports;
    private readonly MutableReactiveProperty<NodeTooltip> tooltip;
    private readonly MutableReactiveProperty<NodeContext?> context;
    
    public IReactiveProperty<NodeTooltip> Tooltip => tooltip;
    public IReactiveProperty<IEnumerable<IPortModel>> Ports => ports;
    public IReactiveProperty<NodeContext?> Context => context;

    public NodeModel(string title, NodeTooltip tooltip = default, string? id = null, string? group = null)
    {
        NodeId = id ?? Guid.NewGuid().ToString();
        Title = title;
        Group = group;
        
        this.tooltip = new MutableReactiveProperty<NodeTooltip>(tooltip);
        ports = new MutableReactiveProperty<List<IPortModel>>(new List<IPortModel>());
        context = new MutableReactiveProperty<NodeContext?>();
    }

    public void AddPort(IPortModel port)
    {
        ports.Value.Add(port);
        ports.Invalidate();
    }

    public void ChangeContext(NodeContext? context)
    {
        this.context.SetValue(context);
    }
    
    public void Dispose()
    {
        ports.Dispose();
        tooltip.Dispose();
        context.Dispose();
    }
}

public readonly struct NodeTooltip
{
    public string Title { get; }
    public string Tip { get; }

    public NodeTooltip(string title, string tip)
    {
        Title = title;
        Tip = tip;
    }
}