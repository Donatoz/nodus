using System;
using System.Linq;
using System.Windows.Input;
using FlowEditor.Models;
using Nodus.Core.Extensions;
using Nodus.Core.Reactive;
using Nodus.DI.Factories;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using ReactiveUI;

namespace FlowEditor.ViewModels;

public class FlowCanvasViewModel : NodeCanvasViewModel
{
    private readonly IFlowCanvasModel model;
    
    public NotifyingBoundProperty<IFlowCanvasExecutable?> FlowExecutable { get; }
    public ICommand StopFlowCommand { get; }
    public ICommand RestartFlowCommand { get; }
    public ICommand DestroyFlowCommand { get; }
    
    public FlowCanvasViewModel(IFlowCanvasModel model, IServiceProvider serviceProvider, IComponentFactoryProvider<NodeCanvasViewModel> elementsFactoryProvider, INodeCanvasViewModelComponentFactory componentFactory) : base(model, serviceProvider, elementsFactoryProvider, componentFactory)
    {
        this.model = model;

        FlowExecutable = model.CurrentlyResolvedFlow.ToNotifying();
        
        StopFlowCommand = ReactiveCommand.Create(OnStopFlow);
        RestartFlowCommand = ReactiveCommand.Create(OnRestartFlow);
        DestroyFlowCommand = ReactiveCommand.Create(OnDestroyFlow);
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
    
    private void OnRestartFlow()
    {
        model.TryRestartFlow();
    }
    
    private void OnDestroyFlow()
    {
        model.TryDestroyFlow();
    }
    
    private void OnStopFlow()
    {
        model.CurrentlyResolvedFlow.Value?.Stop();
    }
}