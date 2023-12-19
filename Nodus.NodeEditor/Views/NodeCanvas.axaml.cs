using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.VisualTree;
using DynamicData;
using DynamicData.Binding;
using Nodus.Core.Common;
using Nodus.Core.Controls;
using Nodus.Core.Extensions;
using Nodus.Core.Reactive;
using Nodus.Core.Utility;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using ISelectable = Nodus.Core.Selection.ISelectable;

namespace Nodus.NodeEditor.Views;

public partial class NodeCanvas : UserControl
{
    private bool isPanning;
    private bool isSelecting;
    private double currentZoom = 1;
    
    //TODO: To config
    private float zoomSpeed = 0.05f;
    private float backgroundTilt = 0.25f;
    private Vector2 zoomMinMax = new(0.65f, 1.4f);

    private Vector2 activeConnectionOffset;
    private Point lastPointerPosition;
    private Point previousCanvasPointerPosition;
    private LineSegment activeConnectionSegment;
    private RectangleGeometry selectionRectGeometry;
    private CompositeDisposable? disposables;

    private readonly ISet<ConnectionContainer> currentConnections;
    private readonly ISet<NodeContainer> nodes;

    private readonly BoundCollectionPresenter<NodeViewModel, Draggable> nodesCollectionView;

    private ScaleTransform CanvasScale =>
        (CanvasesGroup.RenderTransform as TransformGroup).Children[0] as ScaleTransform;
    private TranslateTransform CanvasTranslate =>
        (CanvasesGroup.RenderTransform as TransformGroup).Children[1] as TranslateTransform;
    private VisualBrush BackgroundBrush => BackgroundVisual.Background as VisualBrush;

    public NodeCanvas()
    {
        InitializeComponent();
        InitializeActiveConnection();
        InitializeSelectionRect();

        var tGroup = new TransformGroup();
        
        tGroup.Children.Add(new ScaleTransform());
        tGroup.Children.Add(new TranslateTransform());
        
        CanvasesGroup.RenderTransform = tGroup;

        currentConnections = new HashSet<ConnectionContainer>();
        nodes = new HashSet<NodeContainer>();
        
        nodesCollectionView = new BoundCollectionPresenter<NodeViewModel, Draggable>(CreateNodeControl, CanvasRoot, RemoveNodeControl);
        nodesCollectionView.DestructionPredicate =
            (c, vm) => c is Panel p && p.Children.Any(x => x.DataContext == vm);

        AddHandler(Port.PortPressedEvent, OnPortPressed);
        AddHandler(Port.PortReleasedEvent, OnPortReleased);
        AddHandler(Port.PortDragEvent, OnPortDrag);
        AddHandler(Node.NodePressedEvent, OnNodePressed);
        AddHandler(Draggable.OnDragEvent, OnDraggableDrag);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        disposables?.Dispose();
        disposables = new CompositeDisposable();
        
        if (DataContext is NodeCanvasViewModel vm)
        {
            nodesCollectionView.Repopulate(vm.Nodes.Items);
            nodesCollectionView.Subscribe(vm.Nodes.AlterationStream);
            
            var connectionsStream = vm.Connections.WhenValueChanged(x => x.Value);
            
            disposables.Add(connectionsStream.Subscribe(Observer.Create<IEnumerable<ConnectionViewModel>>(RepopulateConnections)));
        }
        
    }

    #region Nodes Interactions

    protected virtual Draggable CreateNodeControl(NodeViewModel ctx)
    {
        var draggable = new Draggable();

        var pos = lastPointerPosition.Equals(default) 
            ? new Point(400, 0) * (CanvasRoot.Children.OfType<Draggable>().Count() + 1) + new Point(0, 200)
            : lastPointerPosition;
        
        draggable.SetValue(Canvas.LeftProperty, pos.X);
        draggable.SetValue(Canvas.TopProperty, pos.Y);

        var node = new Node { DataContext = ctx };
        
        draggable.Children.Add(node);

        nodes.Add(new NodeContainer(draggable, node));

        return draggable;
    }
    
    private void RemoveNodeControl(Control nodeParent)
    {
        var c = nodes.FirstOrDefault(x => x.Parent == nodeParent);

        if (c.Parent != null)
        {
            nodes.Remove(c);
        }
    }

    private void OnNodePressed(object? sender, NodeEventArgs e)
    {
        if (DataContext is NodeCanvasViewModel vm)
        {
            vm.RequestNodeSelectionCommand.Execute(e.Node.DataContext);
        }
    }
    
    #endregion

    #region Connections Interactions

    private void RepopulateConnections(IEnumerable<ConnectionViewModel> connections)
    {
        currentConnections.Clear();
        ConnectionsCanvas.Children.Clear();
        
        foreach (var connection in connections)
        {
            var c = CreateConnectionControl(connection);
            if (c == null) continue;
            
            currentConnections.Add(c.Value);
            ConnectionsCanvas.Children.Add(c.Value.Path);
            
            UpdateConnection(c.Value);
        }
    }

    protected virtual ConnectionContainer? CreateConnectionControl(ConnectionViewModel connection)
    {
        var path = new ConnectionPath {DataContext = connection};
        var (data, lines) = GeometryUtility.CreatePolyLineGeometry(3);
        path.PathContainer.Data = data;
        
        var sourcePort = nodes.FirstOrDefault(n => n.ViewModel.NodeId == connection.SourceNode.NodeId).Node?
            .FindPort(connection.SourcePort.PortId);
        if (sourcePort == null) return null;
        
        var destPort = nodes.FirstOrDefault(n => n.ViewModel.NodeId == connection.TargetNode.NodeId).Node?
            .FindPort(connection.TargetPort.PortId);
        if (destPort == null) return null;

        return new ConnectionContainer(path, lines, connection, sourcePort, destPort);
    }

    private void UpdateConnections()
    {
        currentConnections.ForEach(UpdateConnection);
    }

    protected virtual void UpdateConnection(ConnectionContainer connection)
    {
        var from = connection.From.GetCenterPoint(ConnectionsCanvas);
        var to = connection.To.GetCenterPoint(ConnectionsCanvas);

        if (from == null || to == null)
        {
            throw new Exception($"Failed to update connection: {connection}. Probably ports are not present in the canvas.");
        }

        const ushort lineFixedSpan = 30;
        
        connection.Path.SetValue(Canvas.LeftProperty, from.Value.X);
        connection.Path.SetValue(Canvas.TopProperty, from.Value.Y);
        
        var direction = (Point) to - (Point) from;

        connection.Lines[0].Point = new Point(lineFixedSpan, 0);
        connection.Lines[1].Point = direction - new Point(lineFixedSpan, 0);
        connection.Lines[2].Point = direction;
    }

    #endregion

    #region Ports Interactions

    protected virtual void InitializeActiveConnection()
    {
        var segments = new PathSegments();
        activeConnectionSegment = new LineSegment();
        segments.Add(activeConnectionSegment);

        var figure = new PathFigure
        {
            Segments = segments
        };
        var figures = new PathFigures { figure };

        ActiveConnection.Data = new PathGeometry { Figures = figures };
    }

    private void OnPortDrag(object? sender, PortDragEventArgs e)
    {
        var pos = e.PointerArgs.GetCurrentPoint(CanvasRoot);
        var point = new Point(pos.Position.X, pos.Position.Y);

        activeConnectionSegment.Point = point - activeConnectionOffset;
    }

    private void OnPortReleased(object? sender, PortDragEventArgs e)
    {
        activeConnectionSegment.Point = default;
        ActiveConnection.IsVisible = false;

        TryConnectToPortAtPoint(e.PointerArgs.GetCurrentPoint(CanvasRoot).Position, e.Port);
    }

    private void TryConnectToPortAtPoint(Point targetPoint, Port sourcePort)
    {
        foreach (var p in nodes.SelectMany(n => n.Node.Ports))
        {
            var portPos = p.PortHandler.TranslatePoint(default, CanvasRoot);
            var portBounds = new Rect((Point)portPos, p.PortHandler.DesiredSize);

            if (portBounds.Contains(targetPoint) && DataContext is NodeCanvasViewModel vm && p != sourcePort)
            {
                var sourceNode = nodes.First(n => n.Node.HasPort(sourcePort.PortId)).Node.NodeId;
                var targetNode = nodes.First(n => n.Node.HasPort(p.PortId)).Node.NodeId;
                
                vm.CreateConnection(sourceNode, sourcePort.PortId, targetNode, p.PortId);
                
                break;
            }
        }
    }

    private void OnPortPressed(object? sender, PortDragEventArgs e)
    {
        if (!e.PointerArgs.GetCurrentPoint(CanvasRoot).Properties.IsLeftButtonPressed) return;

        var portPosition = e.Port.GetCenterPoint(CanvasRoot);

        ActiveConnection.SetValue(Canvas.LeftProperty, portPosition.Value.X);
        ActiveConnection.SetValue(Canvas.TopProperty, portPosition.Value.Y);
        ActiveConnection.IsVisible = true;

        activeConnectionOffset = new Vector2((float)ActiveConnection.GetValue(Canvas.LeftProperty),
            (float)ActiveConnection.GetValue(Canvas.TopProperty));
    }

    #endregion

    #region Viewport Interactions


    #region Selection Rect

    private void InitializeSelectionRect()
    {
        selectionRectGeometry = new RectangleGeometry();
        SelectionRect.Data = selectionRectGeometry;
    }
    
    private void UpdateSelectionRect(PointerEventArgs e)
    {
        var pos = e.GetPosition(CanvasRoot);
        var rectPos = SelectionRect.TranslatePoint(default, CanvasRoot);
        var delta = pos - rectPos;

        selectionRectGeometry.Rect = new Rect(0, 0, delta.Value.X, delta.Value.Y);
    }

    private void StartSelection(Point initialPoint)
    {
        DeselectNodes();

        SelectionRect.IsVisible = true;
            
        SelectionRect.SetValue(Canvas.LeftProperty, initialPoint.X);
        SelectionRect.SetValue(Canvas.TopProperty, initialPoint.Y);
        isSelecting = true;
    }
    
    private void StopSelection()
    {
        SelectionRect.IsVisible = false;
        
        selectionRectGeometry.Rect = default;
    }

    #endregion


    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        isPanning = false;

        if (isSelecting)
        {
            StopSelection();
        }

        isSelecting = false;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!Equals(e.Source, BackgroundVisual)) return;

        var props = e.GetCurrentPoint(CanvasRoot).Properties;
        
        if (props.IsMiddleButtonPressed)
        {
            isPanning = true;
        }
        else if (props.IsLeftButtonPressed)
        {
            StartSelection(e.GetPosition(CanvasRoot));
        }
        else
        {
            DeselectNodes();
        }
        
        previousCanvasPointerPosition = e.GetPosition(CanvasRoot);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        lastPointerPosition = e.GetPosition(CanvasRoot);
        
        if (isPanning)
        {
            if (!Equals(e.GetCurrentPoint(CanvasRoot).Pointer.Captured, BackgroundVisual))
            {
                isPanning = false;
                return;
            }

            var pos = e.GetPosition(CanvasRoot);
            var offset = pos - previousCanvasPointerPosition;
        
            CanvasTranslate.X += offset.X; 
            CanvasTranslate.Y += offset.Y;

            BackgroundBrush.DestinationRect = new RelativeRect(BackgroundBrush.DestinationRect.Rect.Position + offset * backgroundTilt,
                BackgroundBrush.DestinationRect.Rect.Size, BackgroundBrush.DestinationRect.Unit);
        }

        if (isSelecting)
        {
            UpdateSelectionRect(e);
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        
        var effectiveDelta = e.Delta.Y * zoomSpeed;
        
        if (currentZoom + effectiveDelta < zoomMinMax.X || currentZoom + effectiveDelta > zoomMinMax.Y) return;
        
        currentZoom += effectiveDelta;
        CanvasScale.ScaleX += effectiveDelta;
        CanvasScale.ScaleY += effectiveDelta;
        
        BackgroundBrush.DestinationRect = new RelativeRect(BackgroundBrush.DestinationRect.Rect.Position,
            BackgroundBrush.DestinationRect.Rect.Size + new Size(effectiveDelta, effectiveDelta) * 15, BackgroundBrush.DestinationRect.Unit);
    }

    private void OnDraggableDrag(object? sender, DraggableEventArgs e)
    {
        UpdateConnections();
    }

    private void DeselectNodes()
    {
        if (DataContext is NodeCanvasViewModel vm)
        {
            vm.NodesSelector.DeselectAll();
        }
    }

    #endregion

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        disposables?.Dispose();
        nodesCollectionView.Dispose();
    }

    private readonly struct NodeContainer
    {
        public Control Parent { get; }
        public Node Node { get; }
        public NodeViewModel? ViewModel => Node.DataContext as NodeViewModel;

        public NodeContainer(Control parent, Node node)
        {
            Parent = parent;
            Node = node;
        }
    }
    
    protected readonly struct ConnectionContainer
    {
        public ConnectionPath Path { get; }
        public IList<LineSegment> Lines { get; }
        public ConnectionViewModel ViewModel { get; }
        public Port From { get; }
        public Port To { get; }

        public ConnectionContainer(ConnectionPath path, IList<PathSegment> lines, ConnectionViewModel vm, Port from, Port to)
        {
            Path = path;
            Lines = lines.Cast<LineSegment>().ToList();
            ViewModel = vm;
            From = from;
            To = to;
        }

        public override string ToString()
        {
            return $"{From.PortId} -> {To.PortId}";
        }
    }
}