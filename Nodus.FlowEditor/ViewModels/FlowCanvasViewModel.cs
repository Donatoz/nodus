using System;
using System.Linq;
using FlowEditor.Models;
using Nodus.Core.Extensions;
using Nodus.DI.Factories;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace FlowEditor.ViewModels;

public class FlowCanvasViewModel : NodeCanvasViewModel
{
    private readonly IFlowCanvasModel model;
    
    public FlowCanvasViewModel(IFlowCanvasModel model, IServiceProvider serviceProvider, IComponentFactoryProvider<NodeCanvasViewModel> elementsFactoryProvider, INodeCanvasViewModelComponentFactory componentFactory) : base(model, serviceProvider, elementsFactoryProvider, componentFactory)
    {
        this.model = model;
    }

    protected override NodeViewModel CreateNode(INodeModel model)
    {
        var n = base.CreateNode(model);

        n.EventStream.OnEvent<RunFlowEvent>(OnRunFlow);
        
        return n;
    }

    private void OnRunFlow(RunFlowEvent evt)
    {
        model.RunFlowFrom(evt.Root.NodeId);
    }
}