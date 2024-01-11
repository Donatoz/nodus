using System;
using System.Collections.Generic;
using FlowEditor.Models;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace FlowEditor.ViewModels.Contexts;

public class FlowContextContainerViewModel : NodeContextContainerViewModel
{
    public FlowContextContainerViewModel(Func<IEnumerable<INodeModel>> nodesGetter, IObservable<NodeViewModel?> nodeChangeStream) : base(nodesGetter, nodeChangeStream)
    {
    }

    protected override void DescribeContext(INodeContext context)
    {
        if (context is not IFlowContext c) return;
        var vm = new GenericFlowContextViewModel(c);
        
        if (!vm.IsValid) return;
        
        SetContext(new GenericFlowContextViewModel(c));
    }
}