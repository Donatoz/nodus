﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Nodus.Core.Common;
using Nodus.Core.Reactive;
using Nodus.Core.Selection;
using Nodus.Core.ViewModels;
using Nodus.NodeEditor.Models;
using ReactiveUI;

namespace Nodus.NodeEditor.ViewModels;

public class NodeViewModel : ReactiveViewModel, ISelectable
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
    public ICommand DeleteSelf { get; }

    protected readonly INodeModel model;

    public NodeViewModel(INodeModel model)
    {
        this.model = model;
        Title = model.Title;
        NodeId = model.NodeId;
        Tooltip = model.Tooltip.ToBound();
        Group = model.Group;

        Ports = new BoundCollection<IPortModel, PortViewModel>(model.Ports, x => new PortViewModel(x));
        debug = new MutableReactiveProperty<bool>();
        
        AddIntPort = ReactiveCommand.Create(OnAddInPort);
        AddOutPort = ReactiveCommand.Create(OnAddOutPort);
        SwitchDebug = ReactiveCommand.Create(OnSwitchDebug);
        DeleteSelf = ReactiveCommand.Create(OnDeleteSelf);
    }

    private void OnAddInPort()
    {
        model.AddPort(new PortModel("Value", PortType.Input, PortCapacity.Single));
    }
    
    private void OnAddOutPort()
    {
        model.AddPort(new PortModel("Value", PortType.Output, PortCapacity.Multiple));
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