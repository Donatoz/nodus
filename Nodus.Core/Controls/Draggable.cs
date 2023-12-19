using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Nodus.Core.Controls;

public class Draggable : Panel
{
    public static RoutedEvent<DraggableEventArgs> OnDragEvent;
    
    public static readonly StyledProperty<Visual?> BoundaryVisualProperty;
    public static readonly AttachedProperty<bool> ProvideDragProperty;

    private readonly DraggableEventArgs dragArgs;

    public Visual? BoundaryVisual
    {
        get => GetValue(BoundaryVisualProperty);
        set => SetValue(BoundaryVisualProperty, value);
    }
    
    private bool isDragging;
    private Point initialPosition;

    static Draggable()
    {
        OnDragEvent = RoutedEvent.Register<Draggable, DraggableEventArgs>(nameof(OnDragEvent), RoutingStrategies.Bubble);
        BoundaryVisualProperty = AvaloniaProperty.Register<Draggable, Visual?>(nameof(BoundaryVisual));
        ProvideDragProperty = AvaloniaProperty.RegisterAttached<Draggable, Control, bool>("ProvideDrag");
    }

    public Draggable()
    {
        dragArgs = new DraggableEventArgs(this) { RoutedEvent = OnDragEvent };
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var boundary = GetEffectiveBoundary();
        
        if (!e.GetCurrentPoint(boundary).Properties.IsLeftButtonPressed) return;

        var source = e.Source;
        
        if (source is AvaloniaObject obj && !obj.GetValue(ProvideDragProperty)) return;
        
        if (boundary == null) return;
        
        isDragging = true;
        initialPosition = e.GetPosition(boundary);

        var currentPos = GetCurrentPosition();
        
        initialPosition -= new Point(currentPos.X, currentPos.Y);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        isDragging = false;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        
        if (!isDragging) return;

        var boundary = GetEffectiveBoundary();
        
        var pos = e.GetPosition(boundary);
        var newPos = new Point(pos.X - initialPosition.X, pos.Y - initialPosition.Y);
        
        SetValue(Canvas.LeftProperty, newPos.X);
        SetValue(Canvas.TopProperty, newPos.Y);
        
        RaiseEvent(dragArgs);
    }

    private Point GetCurrentPosition()
    {
        return new Point(GetValue(Canvas.LeftProperty), GetValue(Canvas.TopProperty));
    }

    private Visual? GetEffectiveBoundary()
    {
        return BoundaryVisual ?? Parent as Visual;
    }

    public static void SetProvideDrag(AvaloniaObject element, bool provide)
    {
        element.SetValue(ProvideDragProperty, provide);
    }

    public static bool GetProvideDrag(AvaloniaObject element)
    {
        return element.GetValue(ProvideDragProperty);
    }
}

public class DraggableEventArgs : RoutedEventArgs
{
    public Draggable Draggable { get; }

    public DraggableEventArgs(Draggable draggable)
    {
        Draggable = draggable;
    }
}