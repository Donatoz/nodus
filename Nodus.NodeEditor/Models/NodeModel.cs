using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Nodus.Core.Entities;
using Nodus.Core.Reactive;
using Nodus.NodeEditor.Meta;

namespace Nodus.NodeEditor.Models;

public interface INodeModel : IEntity, IPersistentElementModel, IDisposable
{
    string NodeId { get; }
    string Title { get; }
    string? Group { get; }
    
    IReactiveProperty<NodeTooltip> Tooltip { get; }
    IReactiveProperty<IEnumerable<IPortModel>> Ports { get; }
    IReactiveProperty<INodeContext?> Context { get; }

    void AddPort(IPortModel port);
    void ChangeContext(INodeContext? context);
    object? GetPortValue(string portId, GraphContext context);
}

public class NodeModel : Entity, INodeModel
{
    public string NodeId { get; }
    public string ElementId => NodeId;

    public override string EntityId => NodeId;

    public string Title { get; }
    public string? Group { get; }
    private string? ContextId { get; }

    private readonly MutableReactiveProperty<IList<IPortModel>> ports;
    private readonly MutableReactiveProperty<NodeTooltip> tooltip;
    private readonly MutableReactiveProperty<INodeContext?> context;
    
    public IReactiveProperty<NodeTooltip> Tooltip => tooltip;
    public IReactiveProperty<IEnumerable<IPortModel>> Ports => ports;
    public IReactiveProperty<INodeContext?> Context => context;

    private NodeModel()
    {
        ports = new MutableReactiveProperty<IList<IPortModel>>(new List<IPortModel>());
        context = new MutableReactiveProperty<INodeContext?>();
    }

    public NodeModel(string title, NodeTooltip tooltip = default, string? id = null, string? group = null, string? ctxId = null) : this()
    {
        NodeId = id ?? Guid.NewGuid().ToString();
        Title = title;
        Group = group;
        ContextId = ctxId;
        
        this.tooltip = new MutableReactiveProperty<NodeTooltip>(tooltip);
    }

    public void AddPort(IPortModel port)
    {
        ports.Value.Add(port);
        ports.Invalidate();
    }

    public void ChangeContext(INodeContext? context)
    {
        this.context.SetValue(context);
        this.context.Value?.Bind(this);
    }
    
    public object? GetPortValue(string portId, GraphContext context)
    {
        var port = Ports.Value.FirstOrDefault(x => x.Id == portId);

        if (port == null)
        {
            throw new ArgumentException($"Failed to get port value ({portId}): port not found.");
        }

        return Context.Value?.ResolvePortValue(port, context);
    }


    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        if (!disposing) return;
        
        ports.Dispose();
        tooltip.Dispose();
        context.Dispose();
    }

    public virtual IGraphElementData Serialize()
    {
        return new NodeData(Title, Tooltip.Value, Ports.Value.Select(x => x.Serialize()))
        {
            ElementId = NodeId,
            Group = Group,
            ContextId = ContextId,
            ContextData = Context.Value?.Serialize()
        };
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