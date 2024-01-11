using System;
using System.Collections.Generic;
using System.Linq;
using Nodus.Core.Extensions;
using Nodus.Core.Reactive;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.ViewModels;

public class NodeContextContainerViewModel : IDisposable
{
    private readonly MutableReactiveProperty<INodeContextViewModel?> selectedContext;
    private readonly IDisposable changeContract;
    
    private readonly Func<IEnumerable<INodeModel>> nodesGetter; // TODO: Replace with nodes cache pointer
    
    public IReactiveProperty<INodeContextViewModel?> SelectedContext => selectedContext;

    public NodeContextContainerViewModel(Func<IEnumerable<INodeModel>> nodesGetter, IObservable<NodeViewModel?> nodeChangeStream)
    {
        selectedContext = new MutableReactiveProperty<INodeContextViewModel?>();
        this.nodesGetter = nodesGetter;

        changeContract = nodeChangeStream.Subscribe(OnNodeChanged);
    }

    private void OnNodeChanged(NodeViewModel? node)
    {
        if (node == null)
        {
            selectedContext.SetValue(null);
            return;
        }
        
        var context = nodesGetter.Invoke().FirstOrDefault(x => x.NodeId == node.NodeId)
            .NotNull($"Failed to find node with id: {node.NodeId}").Context.Value;
        
        if (context == null) return;
        
        DescribeContext(context);
    }

    protected virtual void DescribeContext(INodeContext context)
    {
    }

    protected void SetContext(INodeContextViewModel contextViewModel)
    {
        selectedContext.SetValue(contextViewModel);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        
        selectedContext.Dispose();
        changeContract.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}