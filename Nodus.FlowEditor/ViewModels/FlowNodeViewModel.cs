using System.Linq;
using FlowEditor.Meta;
using FlowEditor.Models;
using Nodus.Core.Common;
using Nodus.Core.Extensions;
using Nodus.DI.Factories;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace FlowEditor.ViewModels;

public class FlowNodeViewModel : NodeViewModel
{
    private readonly IFlowNodeModel model;
    
    public FlowNodeViewModel(IFlowNodeModel model, 
        IComponentFactoryProvider<NodeCanvasViewModel> componentFactoryProvider, 
        IComponentFactoryProvider<INodeCanvasModel> modelFactoryProvider) : base(model, componentFactoryProvider, modelFactoryProvider)
    {
        this.model = model;
    }

    public void RunFlow()
    {
        RaiseEvent(new RunFlowEvent(this));
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