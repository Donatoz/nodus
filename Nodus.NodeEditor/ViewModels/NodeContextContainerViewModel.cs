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
    private readonly MutableReactiveProperty<IEnumerable<PropertyEditorViewModel>> items;
    private readonly Func<GraphContext> contextProvider;
    private readonly IDisposable changeContract;

    public IReactiveProperty<IEnumerable<PropertyEditorViewModel>> Items => items;
    public BoundProperty<bool> HasValidContext { get; }

    public NodeContextContainerViewModel(Func<GraphContext> contextProvider, IObservable<NodeViewModel?> nodeChangeStream)
    {
        items = new MutableReactiveProperty<IEnumerable<PropertyEditorViewModel>>(Enumerable.Empty<PropertyEditorViewModel>());
        this.contextProvider = contextProvider;
        HasValidContext = new BoundProperty<bool>(() => Items.Value.Any(), Items);

        changeContract = nodeChangeStream.Subscribe(OnNodeChanged);
    }

    private void OnNodeChanged(NodeViewModel? node)
    {
        if (node == null)
        {
            items.SetValue(Enumerable.Empty<PropertyEditorViewModel>());
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
            return new PropertyEditorViewModel(x.Name, x.PropertyType, attr?.Description, CreateEditorBinding(x, context));
        }));
    }
    
    protected virtual IPropertyBinding CreateEditorBinding(PropertyInfo propertyInfo, INodeContext context)
    {
        return new ReflectionPropertyBinding(propertyInfo, context);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        
        items.Dispose();
        changeContract.Dispose();
        HasValidContext.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}