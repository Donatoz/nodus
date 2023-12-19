using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Nodus.Core.Common;
using Nodus.Core.Extensions;
using Nodus.Core.Selection;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace Nodus.NodeEditor.Views;

public partial class Node : UserControl
{
    public static readonly RoutedEvent<NodeEventArgs> NodePressedEvent = 
        RoutedEvent.Register<Node, NodeEventArgs>(nameof(NodePressedEvent), RoutingStrategies.Bubble);
    public SelectableComponent SelectionHandler { get; }

    private IDisposable? portAlterationContract;
    private readonly ISet<Port> ports;

    public IEnumerable<Port> Ports => ports;
    public string NodeId { get; private set; }
    
    public Node()
    {
        SelectionHandler = new SelectableComponent(this);
        ports = new HashSet<Port>();
        NodeId = string.Empty;
        
        InitializeComponent();
        
        Body.AddHandler(PointerPressedEvent, OnPointerPressed);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        portAlterationContract?.Dispose();
        
        if (DataContext is NodeViewModel vm)
        {
            RepopulatePorts();

            NodeId = vm.NodeId;
            portAlterationContract = vm.Ports.AlterationStream.Subscribe(Observer.Create<CollectionChangedEvent<PortViewModel>>(OnPortsChanged));

            if (vm.Group != null)
            {
                NodeGroupBorder.Classes.Clear();
                NodeGroupBorder.Classes.Add(NodeGroups.NodeGroupPrefix + vm.Group.ToLower());
                
                Trace.WriteLine(NodeGroupBorder.Classes.First());
            }
        }
    }

    private void OnPortsChanged(CollectionChangedEvent<PortViewModel> evt)
    {
        if (evt.Added)
        {
            CreatePort(evt.Item);
        }
        else
        {
            RemovePort(evt.Item);
        }
    }

    private void RepopulatePorts()
    {
        InputPortsContainer.Children.Clear();
        OutputPortsContainer.Children.Clear();
        
        if (DataContext is not NodeViewModel vm) return;

        vm.Ports.Items.ForEach(CreatePort);
    }

    private void CreatePort(PortViewModel vm)
    {
        var port = CreatePortControl(vm);

        if (port == null)
        {
            throw new ArgumentException($"Failed to create control for: {vm}");
        }
        
        const int horizontalOffset = -9;
        
        port.Margin = new Thickness(vm.Type == PortType.Input ? horizontalOffset : 0, 10, 
            vm.Type == PortType.Output ? horizontalOffset : 0, 10);
        port.HorizontalAlignment = vm.Type == PortType.Input ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        
        ports.Add(port);
        GetContainerForPort(vm.Type).Children.Add(port);
    }

    private Port? CreatePortControl(PortViewModel vm)
    {
        if (vm.Type == PortType.Input)
        {
            return new InputPort { DataContext = vm };
        }
        if (vm.Type == PortType.Output)
        {
            return new OutputPort { DataContext = vm };
        }

        return null;
    }

    private void RemovePort(PortViewModel vm)
    {
        var p = ports.FirstOrDefault(x => x.DataContext == vm);

        if (p != null)
        {
            GetContainerForPort(vm.Type).Children.Remove(p);
            ports.Remove(p);
        }
    }

    private Panel GetContainerForPort(PortType type)
    {
        return type == PortType.Input ? InputPortsContainer : OutputPortsContainer;
    }
    
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Control c || !c.HasVisualAncestorOrSelf(Container)) return;
        
        RaiseEvent(new NodeEventArgs(this) {RoutedEvent = NodePressedEvent});
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        InputPortsContainer.Margin = new Thickness(InputPortsContainer.Margin.Left, NodeHeader.DesiredSize.Height + 15,
            InputPortsContainer.Margin.Right, InputPortsContainer.Margin.Bottom);
        OutputPortsContainer.Margin = new Thickness(OutputPortsContainer.Margin.Left, NodeHeader.DesiredSize.Height + 15,
            OutputPortsContainer.Margin.Right, OutputPortsContainer.Margin.Bottom);
        
        return base.ArrangeOverride(finalSize);
    }

    public Port? FindPort(string portId)
    {
        return ports.FirstOrDefault(x => x.PortId == portId);
    }

    public bool HasPort(string portId)
    {
        return ports.Any(p => p.PortId == portId);
    }
    
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        
        SelectionHandler.Dispose();
        portAlterationContract?.Dispose();
    }
}

public class NodeEventArgs : RoutedEventArgs
{
    public Node Node { get; }

    public NodeEventArgs(Node node)
    {
        Node = node;
    }
}