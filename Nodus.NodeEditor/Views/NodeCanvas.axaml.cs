using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reactive.Disposables;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.ReactiveUI;
using DynamicData.Binding;
using Nodus.Core.Controls;
using Nodus.Core.Extensions;
using Nodus.Core.Factories;
using Nodus.Core.Utility;
using Nodus.Core.ViewModels;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using Nodus.NodeEditor.ViewModels.Events;
using Nodus.NodeEditor.Views.Templates;
using ReactiveUI;

namespace Nodus.NodeEditor.Views;

//TODO: Split this hell into different partial classes (one for nodes logic, one for connections, etc...)
public partial class NodeCanvas : ReactiveUserControl<NodeCanvasViewModel>
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
    private Point previousCreatedElementPosition;

    protected readonly ISet<ConnectionContainer> currentConnections;
    protected readonly ISet<ElementContainer> elements;
    
    public ICommand ResetPositionCommand { get; }
    public ICommand AddNodeCommand { get; }

    private ScaleTransform CanvasScale =>
        (CanvasesGroup.RenderTransform as TransformGroup).Children[0] as ScaleTransform;
    private TranslateTransform CanvasTranslate =>
        (CanvasesGroup.RenderTransform as TransformGroup).Children[1] as TranslateTransform;
    private VisualBrush BackgroundBrush => BackgroundVisual.Background as VisualBrush;
    
    protected IFactoryProvider<NodeCanvas> FactoryProvider { get; }
    protected Panel CanvasContainer => Root;

    public NodeCanvas(IFactoryProvider<NodeCanvas> factoryProvider)
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
        elements = new HashSet<ElementContainer>();

        ResetPositionCommand = ReactiveCommand.Create(OnResetPosition);
        AddNodeCommand = ReactiveCommand.Create(OnAddNode);

        this.WhenActivated(d =>
        {
            ViewModel?.Elements
                .ToObservableChangeSet()
                .TunnelChanges(ProcessElementAddition, ProcessElementRemoval)
                .Subscribe()
                .DisposeWith(d);

            ViewModel?.Connections
                .ToObservableChangeSet()
                .TunnelChanges(ProcessConnectionAddition, ProcessConnectionRemoval)
                .Subscribe()
                .DisposeWith(d);
        });
        
        AddHandler(Port.PortPressedEvent, OnPortPressed);
        AddHandler(Port.PortReleasedEvent, OnPortReleased);
        AddHandler(Port.PortDragEvent, OnPortDrag);
        AddHandler(GraphElement.ElementPressedEvent, OnElementPressed);
        AddHandler(Draggable.OnDragEvent, OnDraggableDrag);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        DataTemplates.Add(CreateComponentSelector());

        initialBackgroundRect = BackgroundBrush.DestinationRect;
        
        Root.Children.Add(this.CreateExtensionControl<ModalContainer, ModalCanvasViewModel>());
        Root.Children.Add(this.CreateExtensionControl<PopupContainer, PopupContainerViewModel>());
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        disposables?.Dispose();
        disposables = new CompositeDisposable();
        
        if (DataContext is NodeCanvasViewModel vm)
        {
            disposables.Add(vm.EventStream.OnEvent<ElementVisualMutationEvent>(OnElementVisualDataMutation));
        }
        
    }

    protected virtual IDataTemplate CreateComponentSelector()
    {
        return new NodeCanvasComponentsSelector();
    }

    #region Elements

    private void ProcessElementAddition(ElementViewModel element)
    {
        var wrapper = CreateElementWrapper(element);

        GraphElement? elementControl = element switch
        {
            NodeViewModel n => CreateNodeControl(n, wrapper),
            CommentViewModel c => CreateCommentControl(c),
            _ => null
        };

        if (elementControl == null)
        {
            throw new Exception($"Failed to create control for element: {element}");
        }

        elements.Add(new ElementContainer(wrapper, elementControl));
        wrapper.Children.Add(elementControl);
        CanvasRoot.Children.Add(wrapper);
    }

    private void ProcessElementRemoval(ElementViewModel element)
    {
        var container = elements.First(x => x.ViewModel == element);
        
        container.Element.DestroySelf(() => CanvasRoot.Children.Remove(container.Parent));
        elements.Remove(container);
    }

    protected virtual Panel CreateElementWrapper(ElementViewModel element)
    {
        var draggable = new Draggable();
        
        Point? dataPos = element.VisualData != null ? new Point(element.VisualData.Position.X, element.VisualData.Position.Y) : null;

        var pos = dataPos ?? (lastPointerPosition.Equals(default) 
            ? new Point(400, 0) * (CanvasRoot.Children.OfType<Draggable>().Count() + 1) + new Point(0, 200)
            : lastPointerPosition);

        if (pos == previousCanvasPointerPosition)
        {
            pos += new Point(100, 0);
        }
        
        draggable.SetValue(Canvas.LeftProperty, pos.X);
        draggable.SetValue(Canvas.TopProperty, pos.Y);

        previousCanvasPointerPosition = pos;

        return draggable;
    }

    #endregion

    #region Nodes Interactions

    protected virtual Node CreateNodeControl(NodeViewModel ctx, Control parent)
    {
        var node = FactoryProvider.GetFactory<IControlFactory<Node, NodeViewModel>>().Create(ctx);
        
        return node;
    }

    private void OnElementPressed(object? sender, ElementPressedEventArgs e)
    {
        if (DataContext is NodeCanvasViewModel vm)
        {
            vm.RequestElementSelectionCommand.Execute(new ElementSelectionRequest(e.Element.DataContext as ElementViewModel,
                e.Modifiers == KeyModifiers.Shift));
        }
    }
    
    private void OnAddNode()
    {
        if (DataContext is not NodeCanvasViewModel vm) return;
        
        vm.AddNodeCommand.Execute(null);
    }

    private Node FindNode(string id)
    {
        return elements.First(x => x.ViewModel is NodeViewModel vm && vm.NodeId == id).Element
            .MustBe<Node>();
    }

    private IEnumerable<Node> GetNodes()
    {
        return elements.Where(x => x.ViewModel is NodeViewModel).Select(x => x.Element.MustBe<Node>());
    }
    
    #endregion

    #region Comments

    protected virtual Comment CreateCommentControl(CommentViewModel vm)
    {
        return FactoryProvider.GetFactory<IControlFactory<Comment, CommentViewModel>>().Create(vm);
    }

    #endregion

    #region Connections Interactions

    private void ProcessConnectionAddition(ConnectionViewModel connection)
    {
        var connectionControl = CreateConnectionControl(connection);
        ConnectionsCanvas.Children.Add(connectionControl);
    }

    private void ProcessConnectionRemoval(ConnectionViewModel connection)
    {
        ConnectionsCanvas.Children.Remove(currentConnections.First(x => x.ViewModel == connection).Path);
        RemoveConnectionControl(connection);
    }
    
    protected virtual ConnectionPath CreateConnectionControl(ConnectionViewModel connection)
    {
        var path = FactoryProvider.GetFactory<IControlFactory<ConnectionPath, ConnectionViewModel>>().Create(connection);
        var (data, lines) = GeometryUtility.CreatePolyLineGeometry(3);
        path.PathContainer.Data = data;
        
        var sourcePort = FindNode(connection.SourceNode.NodeId).FindPort(connection.SourcePort.PortId);
        if (sourcePort == null) return null;
        
        var destPort = FindNode(connection.TargetNode.NodeId).FindPort(connection.TargetPort.PortId);
        if (destPort == null) return null;

        var container = new ConnectionContainer(path, lines, connection, sourcePort, destPort, connection.Data);
        currentConnections.Add(container);

        UpdateConnection(container);

        return path;
    }
    
    private void RemoveConnectionControl(ConnectionViewModel connection)
    {
        currentConnections.Remove(currentConnections.First(x => x.ViewModel == connection));
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
        foreach (var p in GetNodes().Select(n => n.Ports).SelectMany(x => x))
        {
            var portPos = p.PortHandler.TranslatePoint(default, CanvasRoot);
            var portBounds = new Rect((Point)portPos, p.PortHandler.DesiredSize);

            if (portBounds.Contains(targetPoint) && DataContext is NodeCanvasViewModel vm && p != sourcePort)
            {
                var sourceNode = GetNodes().First(n => n.HasPort(sourcePort.PortId)).NodeId;
                var targetNode = GetNodes().First(n => n.HasPort(p.PortId)).NodeId;
                
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
        if (!Equals(e.Source, PointerArea)) return;

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
            if (!Equals(e.GetCurrentPoint(CanvasRoot).Pointer.Captured, PointerArea))
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
            vm.ElementSelector.DeselectAll();
        }
    }

    #endregion

    #region Serialization

    protected virtual void OnElementVisualDataMutation(ElementVisualMutationEvent evt)
    {
        var container = elements.FirstOrDefault(x => x.ViewModel == evt.ViewModel);

        if (container.ViewModel == null)
        {
            throw new Exception($"Failed to mutate element visual data: element not found ({evt.ViewModel.ElementId})");
        }

        var pos = container.Parent.TranslatePoint(default, CanvasRoot);
        
        evt.MutatedValue.Position = new Vector2((float)(pos?.X ?? 0), (float)(pos?.Y ?? 0));
    }

    #endregion

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        disposables?.Dispose();
    }

    protected readonly struct ElementContainer
    {
        public Control Parent { get; }
        public GraphElement Element { get; }
        public ElementViewModel? ViewModel => Element.DataContext as ElementViewModel;

        public ElementContainer(Control parent, GraphElement element)
        {
            Parent = parent;
            Element = element;
        }
    }
    
    protected record ConnectionContainer
    {
        public Connection Data { get; }
        public ConnectionPath Path { get; }
        public IList<LineSegment> Lines { get; }
        public ConnectionViewModel ViewModel { get; }
        public Port From { get; }
        public Port To { get; }

        public ConnectionContainer(ConnectionPath path, IList<PathSegment> lines, ConnectionViewModel vm, Port from, Port to, Connection data)
        {
            Path = path;
            Lines = lines.Cast<LineSegment>().ToList();
            ViewModel = vm;
            From = from;
            To = to;
            Data = data;
        }

        public override string ToString()
        {
            return $"{From.PortId} -> {To.PortId}";
        }
    }
}