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
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Nodus.Core.Common;
using Nodus.Core.Extensions;
using Nodus.Core.Selection;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using ReactiveUI;

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

    protected ContextMenu Menu => NodeContextMenu;
    protected Control NodeBody => Body;

    private LinearGradientBrush borderAscent;
    private GradientStop borderAscentStop;
    private LinearGradientBrush backgroundAscent;
    private GradientStop backgroundAscentStop;
    
    public Node()
    {
        SelectionHandler = new SelectableComponent(this);
        ports = new HashSet<Port>();
        NodeId = string.Empty;
        
        InitializeAscentColors();
        InitializeComponent();
        
        Body.AddHandler(PointerPressedEvent, OnPointerPressed);
    }

    private void InitializeAscentColors()
    {
        borderAscentStop = new GradientStop(default, 1);
        borderAscent = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(default, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new(Color.Parse("#404040"), 0),
                borderAscentStop
            }
        };

        backgroundAscentStop = new GradientStop(default, 1);
        backgroundAscent = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0.1, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new(Color.Parse("#2e2e2e"), 0),
                backgroundAscentStop
            }
        };
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
            }
        }
    }

    protected void UpdateAscents()
    {
        var brush = NodeGroupBorder.Background;
        
        if (brush is not ISolidColorBrush b) return;

        var style = new Style(x => x.OfType<Border>().Class("active"));

        borderAscentStop.Color = b.Color;
        style.Setters.Add(new Setter(Border.BorderBrushProperty, borderAscent));

        var backgroundAscent = b.Color.Lerp(Color.Parse("#2e2e2e"), 0.94f).ToHsv();
        backgroundAscent = new HsvColor(backgroundAscent.A, backgroundAscent.H, backgroundAscent.S * 2f,
            backgroundAscent.V);

        backgroundAscentStop.Color = backgroundAscent.ToRgb();
        style.Setters.Add(new Setter(Border.BackgroundProperty, this.backgroundAscent));
        
        Body.Styles.Add(style);
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
        
        ports.Add(port);
        GetContainerForPort(vm.Type).Children.Add(port);
    }

    protected virtual Port? CreatePortControl(PortViewModel vm)
    {
        return vm.Type switch
        {
            PortType.Input => new InputPort { DataContext = vm },
            PortType.Output => new OutputPort { DataContext = vm },
            _ => null
        };
    }

    protected void RemovePort(PortViewModel vm)
    {
        var p = ports.FirstOrDefault(x => x.DataContext == vm);

        if (p != null)
        {
            GetContainerForPort(vm.Type).Children.Remove(p);
            ports.Remove(p);
        }
    }

    protected Panel GetContainerForPort(PortType type)
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
        
        UpdateAscents();
        
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