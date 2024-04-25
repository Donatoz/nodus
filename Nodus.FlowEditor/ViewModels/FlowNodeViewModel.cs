using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using DynamicData;
using FlowEditor.Meta;
using FlowEditor.Models;
using FlowEditor.Models.Extensions;
using Nodus.Core.Common;
using Nodus.Core.Extensions;
using Nodus.Core.Reactive;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using ReactiveUI;

namespace FlowEditor.ViewModels;

public class FlowNodeViewModel : NodeViewModel
{
    public NotifyingBoundProperty<IFlowResolveContext?> CurrentResolveContext { get; }

    private readonly ReadOnlyObservableCollection<ContextExtensionViewModel> extensions;
    public ReadOnlyObservableCollection<ContextExtensionViewModel> Extensions => extensions;
    
    private new readonly IFlowNodeModel model;
    
    private readonly IDisposable contextContract;
    private readonly IDisposable extensionsContract;
    
    public FlowNodeViewModel(IFlowNodeModel model, 
        IFactoryProvider<NodeCanvasViewModel> componentFactoryProvider, 
        IFactoryProvider<INodeCanvasModel> modelFactoryProvider) : base(model, componentFactoryProvider, modelFactoryProvider)
    {
        this.model = model;
        CurrentResolveContext = new NotifyingBoundProperty<IFlowResolveContext?>(() => model.TryGetFlowContext()?.CurrentResolveContext.Value);

        extensionsContract = model.ContextExtensions
            .Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Transform(x => new ContextExtensionViewModel(x, () => model.ContextExtensions.Remove(x)))
            .Bind(out extensions)
            .Subscribe();

        contextContract = model.Context.AlterationStream.Subscribe(OnContextChanged);
    }

    private void OnContextChanged(INodeContext? context)
    {
        CurrentResolveContext.ClearSources();
        
        if (context is IFlowContext flowCtx)
        {
            CurrentResolveContext.AddSource(flowCtx.CurrentResolveContext);
        }
    }

    public void RunFlow()
    {
        RaiseEvent(new RunFlowEvent(this));
    }

    public void AddExtension()
    {
        model.ContextExtensions.Add(new WaitExtension(TimeSpan.FromSeconds(3)));
    }
    
    protected override void OnAddInPort()
    {
        model.AddPort(ModelFactoryProvider.GetFactory<IPortModelFactory>()
            .CreatePort(new FlowPortData("Value", PortType.Input, PortCapacity.Single, typeof(FlowType))));
    }
    
    protected override void OnAddOutPort()
    {
        model.AddPort(ModelFactoryProvider.GetFactory<IPortModelFactory>()
            .CreatePort(new FlowPortData("Value", PortType.Output, PortCapacity.Multiple, typeof(FlowType))));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        if (!disposing) return;
        
        Ports.Items.OfType<FlowPortViewModel>().DisposeAll();
        contextContract.Dispose();
        extensionsContract.Dispose();
    }
}

public readonly struct RunFlowEvent : IEvent
{
    public FlowNodeViewModel Root { get; }

    public RunFlowEvent(FlowNodeViewModel root)
    {
        Root = root;
    }
}