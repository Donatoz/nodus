using System;
using System.Collections.Generic;
using FlowEditor.ViewModels;
using FlowEditor.ViewModels.Contexts;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace FlowEditor.Factories;

public class FlowCanvasViewModelComponentFactory : NodeCanvasViewModelComponentFactory
{
    public override NodeContextContainerViewModel CreateNodeContextContainer(Func<IEnumerable<INodeModel>> nodesGetter, IObservable<NodeViewModel?> nodeChangeStream)
    {
        return new FlowContextContainerViewModel(nodesGetter, nodeChangeStream, FlowContextViewModelFactory.Default);
    }

    public override NodeCanvasToolbarViewModel CreateToolbar(IServiceProvider serviceProvider, INodeCanvasModel canvasModel,
        INodeCanvasViewModel viewModel)
    {
        return new FlowCanvasToolbarViewModel(serviceProvider, canvasModel, viewModel);
    }
}