using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Nodus.Core.Extensions;
using Nodus.Core.ObjectDescription;
using Nodus.Core.Reactive;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.ViewModels;

public class NodeContextContainerViewModel : IDisposable
{
    private readonly MutableReactiveProperty<IEnumerable<NodeContextContainerItemViewModel>> items;
    private readonly Func<GraphContext> contextProvider;
    private readonly IDisposable changeContract;

    public IReactiveProperty<IEnumerable<NodeContextContainerItemViewModel>> Items => items;

    public NodeContextContainerViewModel(Func<GraphContext> contextProvider, IObservable<NodeViewModel?> nodeChangeStream)
    {
        items = new MutableReactiveProperty<IEnumerable<NodeContextContainerItemViewModel>>();
        this.contextProvider = contextProvider;

        changeContract = nodeChangeStream.Subscribe(OnNodeChanged);
    }

    private void OnNodeChanged(NodeViewModel? node)
    {
        if (node == null)
        {
            items.SetValue(Enumerable.Empty<NodeContextContainerItemViewModel>());
            return;
        }
        
        var graph = contextProvider.Invoke();
        var context = graph.FindNode(node.NodeId)
            .NotNull($"Failed to find node with id: {node.NodeId}").Context.Value;
        
        if (context == null) return;
        
        DescribeContext(context);
    }

    protected virtual void DescribeContext(INodeContext context)
    {
        var props = context.GetType().GetProperties()
            .Where(x => x.IsDefined(typeof(ExposedPropertyAttribute), false));

        items.SetValue(props.Select(x =>
        {
            var attr = x.GetCustomAttribute<ExposedPropertyAttribute>();
            return new NodeContextContainerItemViewModel(x.Name, x.PropertyType, attr?.Description);
        }));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        
        items.Dispose();
        changeContract.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

public class NodeContextContainerItemViewModel
{
    public string PropertyName { get; }
    public string? Description { get; }
    public Type PropertyType { get; }

    public NodeContextContainerItemViewModel(string propertyName, Type propertyType, string? description = null)
    {
        PropertyName = propertyName;
        PropertyType = propertyType;
        Description = description;
    }
}