using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using DynamicData.Binding;
using FlowEditor.Models;
using Nodus.Core.Entities;
using Nodus.Core.Extensions;
using Nodus.Core.Reactive;
using Nodus.Core.ViewModels;
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
    private readonly CompositeDisposable disposables;
    
    public NotifyingBoundProperty<IFlowCanvasExecutable?> FlowExecutable { get; }
    public ICommand StopFlowCommand { get; }
    public ICommand RestartFlowCommand { get; }
    public ICommand DestroyFlowCommand { get; }
    
    public FlowCanvasViewModel(IFlowCanvasModel model, IServiceProvider serviceProvider, 
        IFactory<IGraphElementModel, ElementViewModel> elementsFactory, 
        INodeCanvasViewModelComponentFactory componentFactory, IFlowLogger logger) : base(model, serviceProvider, componentFactory, elementsFactory)
    {
        this.model = model;
        disposables = new CompositeDisposable();

        FlowExecutable = model.CurrentlyResolvedFlow.ToNotifying();
        
        StopFlowCommand = ReactiveCommand.Create(OnStopFlow);
        RestartFlowCommand = ReactiveCommand.Create(OnRestartFlow);
        DestroyFlowCommand = ReactiveCommand.Create(OnDestroyFlow);

        Elements
            .ToObservableChangeSet()
            .TunnelAdditions(OnElementAdded)
            .Subscribe()
            .DisposeWith(disposables);

        this.AddComponent(new DisposableContainer<LogViewModel>(new LogViewModel(logger)));
    }

    private void OnElementAdded(ElementViewModel element)
    {
        element.EventStream
            .OfType<RunFlowEvent>()
            .Subscribe(OnRunFlow)
            .DisposeWith(disposables);
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

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        if (!disposing) return;
        
        disposables.Dispose();
    }
}