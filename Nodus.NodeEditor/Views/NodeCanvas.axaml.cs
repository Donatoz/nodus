using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using DynamicData.Binding;
using Ninject;
using Nodus.Core.Common;
using Nodus.Core.Controls;
using Nodus.Core.Extensions;
using Nodus.Core.Factories;
using Nodus.Core.Reactive;
using Nodus.Core.Utility;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.ViewModels;
using Nodus.NodeEditor.ViewModels.Events;
using ReactiveUI;

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
    private RelativeRect initialBackgroundRect;

    private readonly ISet<ConnectionContainer> currentConnections;
    private readonly ISet<NodeContainer> nodes;

    private readonly BoundCollectionPresenter<NodeViewModel, Draggable> nodesCollectionView;
    private readonly BoundCollectionPresenter<ConnectionViewModel, ConnectionPath> connectionsCollectionView;
    
    public ICommand ResetPositionCommand { get; }

    private ScaleTransform CanvasScale =>
        (CanvasesGroup.RenderTransform as TransformGroup).Children[0] as ScaleTransform;
    private TranslateTransform CanvasTranslate =>
        (CanvasesGroup.RenderTransform as TransformGroup).Children[1] as TranslateTransform;
    private VisualBrush BackgroundBrush => BackgroundVisual.Background as VisualBrush;
    
    protected IComponentFactoryProvider<NodeCanvas> FactoryProvider { get; }

    public NodeCanvas(IComponentFactoryProvider<NodeCanvas> factoryProvider)
    {
        FactoryProvider = factoryProvider;
        
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

        connectionsCollectionView =
            new BoundCollectionPresenter<ConnectionViewModel, ConnectionPath>(CreateConnectionControl,
                ConnectionsCanvas, RemoveConnectionControl);

        ResetPositionCommand = ReactiveCommand.Create(OnResetPosition);

        AddHandler(Port.PortPressedEvent, OnPortPressed);
        AddHandler(Port.PortReleasedEvent, OnPortReleased);
        AddHandler(Port.PortDragEvent, OnPortDrag);
        AddHandler(Node.NodePressedEvent, OnNodePressed);
        AddHandler(Draggable.OnDragEvent, OnDraggableDrag);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        initialBackgroundRect = BackgroundBrush.DestinationRect;
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
            connectionsCollectionView.Repopulate(vm.Connections.Items);
            connectionsCollectionView.Subscribe(vm.Connections.AlterationStream);
            
            disposables.Add(vm.EventStream.OnEvent<NodeVisualMutationEvent>(OnNodeVisualDataMutation));
        }
        
    }

    #region Nodes Interactions

    protected virtual Draggable CreateNodeControl(NodeViewModel ctx)
    {
        var draggable = new Draggable();

        var pos = ctx.VisualData?.Position 
                  ?? (lastPointerPosition.Equals(default) 
                    ? new Point(400, 0) * (CanvasRoot.Children.OfType<Draggable>().Count() + 1) + new Point(0, 200)
                    : lastPointerPosition);
        
        draggable.SetValue(Canvas.LeftProperty, pos.X);
        draggable.SetValue(Canvas.TopProperty, pos.Y);

        var node = FactoryProvider.GetFactory<IControlFactory<Node, NodeViewModel>>().Create(ctx);
        
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

    protected virtual ConnectionPath CreateConnectionControl(ConnectionViewModel connection)
    {
        var path = FactoryProvider.GetFactory<IControlFactory<ConnectionPath, ConnectionViewModel>>().Create(connection);
        var (data, lines) = GeometryUtility.CreatePolyLineGeometry(3);
        path.PathContainer.Data = data;
        
        var sourcePort = nodes.FirstOrDefault(n => n.ViewModel.NodeId == connection.SourceNode.NodeId).Node?
            .FindPort(connection.SourcePort.PortId);
        if (sourcePort == null) return null;
        
        var destPort = nodes.FirstOrDefault(n => n.ViewModel.NodeId == connection.TargetNode.NodeId).Node?
            .FindPort(connection.TargetPort.PortId);
        if (destPort == null) return null;

        var container = new ConnectionContainer(path, lines, connection, sourcePort, destPort);
        currentConnections.Add(container);

        UpdateConnection(container);

        return path;
    }
    
    private void RemoveConnectionControl(Control connectionControl)
    {
        var c = currentConnections.FirstOrDefault(x => x.Path == connectionControl);

        if (c.Path != null)
        {
            currentConnections.Remove(c);
        }
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
            return;
        }
        
        connection.Path.SetValue(Canvas.LeftProperty, from.Value.X);
        connection.Path.SetValue(Canvas.TopProperty, from.Value.Y);
        connection.Path.UpdatePath((Point) from, (Point) to, connection.Lines);
        connection.Path.UpdateColor(connection.From.HandlerColor, connection.To.HandlerColor);
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
        ActiveConnection.Stroke = new ImmutableSolidColorBrush(e.Port.HandlerColor);
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
    
    private void OnResetPosition()
    {
        currentZoom = 1;
        CanvasTranslate.X = 0; 
        CanvasTranslate.Y = 0;
        CanvasScale.ScaleX = 1;
        CanvasScale.ScaleY = 1;
        BackgroundBrush.DestinationRect = initialBackgroundRect;
    }

    protected virtual void OnDraggableDrag(object? sender, DraggableEventArgs e)
    {
        UpdateConnections();
    }

    protected void DeselectNodes()
    {
        if (DataContext is NodeCanvasViewModel vm)
        {
            vm.NodesSelector.DeselectAll();
        }
    }

    #endregion

    #region Serialization

    protected virtual void OnNodeVisualDataMutation(NodeVisualMutationEvent evt)
    {
        var container = nodes.FirstOrDefault(x => x.ViewModel == evt.ViewModel);

        if (container.Node == null)
        {
            throw new Exception($"Failed to mutate node visual data: node not found ({evt.ViewModel.NodeId})");
        }

        var pos = container.Node.TranslatePoint(default, CanvasRoot);
        
        evt.MutatedValue.Position = (Point) pos;
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