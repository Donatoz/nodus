using System.Diagnostics;
using System.Windows.Input;
using Nodus.Core.Common;
using Nodus.Core.Entities;
using Nodus.Core.Reactive;
using Nodus.Core.Selection;
using Nodus.Core.ViewModels;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using ReactiveUI;

namespace Nodus.NodeEditor.ViewModels;

public class NodeViewModel : ElementViewModel
{
    public string Title { get; }
    public string NodeId { get; }
    public string? Group { get; }

    public BoundProperty<NodeTooltip> Tooltip { get; }
    public BoundCollection<IPortModel, PortViewModel> Ports { get; }

    private readonly MutableReactiveProperty<bool> debug;
    public IReactiveProperty<bool> Debug => debug;
    
    public ICommand AddIntPort { get; }
    public ICommand AddOutPort { get; }
    public ICommand SwitchDebug { get; }

    protected readonly INodeModel model;
    protected IFactoryProvider<INodeCanvasModel> ModelFactoryProvider { get; }

    public NodeViewModel(INodeModel model, 
        IFactoryProvider<NodeCanvasViewModel> componentFactoryProvider,
        IFactoryProvider<INodeCanvasModel> modelFactoryProvider) : base(model)
    {
        this.model = model;
        Title = model.Title;
        NodeId = model.NodeId;
        Tooltip = model.Tooltip.ToBound();
        Group = model.Group;

        ModelFactoryProvider = modelFactoryProvider;

        Ports = new BoundCollection<IPortModel, PortViewModel>(model.Ports, 
            componentFactoryProvider.GetFactory<IPortViewModelFactory>().Create);
        debug = new MutableReactiveProperty<bool>();
        
        AddIntPort = ReactiveCommand.Create(OnAddInPort);
        AddOutPort = ReactiveCommand.Create(OnAddOutPort);
        SwitchDebug = ReactiveCommand.Create(OnSwitchDebug);
    }

    protected virtual void OnAddInPort()
    {
        model.AddPort(ModelFactoryProvider.GetFactory<IPortModelFactory>()
            .CreatePort(new PortData("Value", PortType.Input, PortCapacity.Single)));
    }
    
    protected virtual void OnAddOutPort()
    {
        model.AddPort(ModelFactoryProvider.GetFactory<IPortModelFactory>()
            .CreatePort(new PortData("Value", PortType.Output, PortCapacity.Multiple)));
    }

    private void OnSwitchDebug()
    {
        debug.SetValue(!debug.Value);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            Tooltip.Dispose();
            Ports.Dispose();
            debug.Dispose();
        }
    }
}