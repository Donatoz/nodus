using System;
using System.Collections.Generic;
using FlowEditor.Models;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace FlowEditor.ViewModels.Contexts;

public class FlowContextContainerViewModel : NodeContextContainerViewModel
{
    private readonly IFactory<IFlowContext, IFlowContextViewModel> contextVmFactory;
    
    public FlowContextContainerViewModel(Func<IEnumerable<INodeModel>> nodesGetter, IObservable<NodeViewModel?> nodeChangeStream,
        IFactory<IFlowContext, IFlowContextViewModel> contextVmFactory) : base(nodesGetter, nodeChangeStream)
    {
        this.contextVmFactory = contextVmFactory;
    }

    protected override void DescribeContext(INodeContext context)
    {
        if (context is not IFlowContext c) return;
        
        var vm = contextVmFactory.Create(c);
        
        if (!vm.IsValid) return;
        
        SetContext(vm);
    }
}