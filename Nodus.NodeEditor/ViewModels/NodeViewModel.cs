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

public class NodeViewModel : ReactiveViewModel, ISelectable
{
    public string Title { get; }
    public string NodeId { get; }
    public string? Group { get; }
    public NodeVisualData? VisualData { get; protected set; }

    public BoundProperty<NodeTooltip> Tooltip { get; }
    public BoundCollection<IPortModel, PortViewModel> Ports { get; }

    private readonly MutableReactiveProperty<bool> debug;
    public IReactiveProperty<bool> Debug => debug;
    
    public ICommand AddIntPort { get; }
    public ICommand AddOutPort { get; }
    public ICommand SwitchDebug { get; }
    public ICommand DeleteSelf { get; }

    protected readonly INodeModel model;
    protected IComponentFactoryProvider<INodeCanvasModel> ModelFactoryProvider { get; }

    public NodeViewModel(INodeModel model, 
        IComponentFactoryProvider<NodeCanvasViewModel> componentFactoryProvider,
        IComponentFactoryProvider<INodeCanvasModel> modelFactoryProvider)
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

        if (model.TryGetComponent(out ValueContainer<NodeData> data))
        {
            ApplyData(data.Value);
        }
        
        AddIntPort = ReactiveCommand.Create(OnAddInPort);
        AddOutPort = ReactiveCommand.Create(OnAddOutPort);
        SwitchDebug = ReactiveCommand.Create(OnSwitchDebug);
        DeleteSelf = ReactiveCommand.Create(OnDeleteSelf);
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

    private void OnDeleteSelf()
    {
        RaiseEvent(new NodeDeleteRequest(this));
    }

    private void OnSwitchDebug()
    {
        debug.SetValue(!debug.Value);
    }

    public virtual void Select()
    {
        RaiseEvent(new SelectionEvent(this, true));
    }

    public virtual void Deselect()
    {
        RaiseEvent(new SelectionEvent(this, false));
    }

    protected virtual void ApplyData(NodeData data)
    {
        VisualData = data.VisualData;
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

public readonly struct NodeDeleteRequest : IEvent
{
    public NodeViewModel Node { get; }

    public NodeDeleteRequest(NodeViewModel node)
    {
        Node = node;
    }
}