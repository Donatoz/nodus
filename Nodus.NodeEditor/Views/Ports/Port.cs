using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Nodus.NodeEditor.ViewModels;

namespace Nodus.NodeEditor.Views;

public abstract class Port : UserControl
{
    public static readonly RoutedEvent<PortDragEventArgs> PortPressedEvent;
    public static readonly RoutedEvent<PortEventArgs> PortReleasedEvent;
    public static readonly RoutedEvent<PortDragEventArgs> PortDragEvent;
    
    protected bool IsDragging { get; private set; }
    
    public abstract Layoutable PortHandler { get; }
    public string PortId { get; private set; }

    static Port()
    {
        PortPressedEvent = RoutedEvent.Register<Port, PortDragEventArgs>(nameof(PortPressedEvent), RoutingStrategies.Bubble);
        PortReleasedEvent = RoutedEvent.Register<Port, PortEventArgs>(nameof(PortReleasedEvent), RoutingStrategies.Bubble);
        PortDragEvent = RoutedEvent.Register<Port, PortDragEventArgs>(nameof(PortDragEvent), RoutingStrategies.Bubble);
    }

    protected Port()
    {
        PortId = string.Empty;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is PortViewModel vm)
        {
            PortId = vm.PortId;
        }
    }

    protected override void OnInitialized()
    {
        AddHandler(PointerPressedEvent, OnPointerPressed);
        AddHandler(PointerReleasedEvent, OnPointerReleased);
        AddHandler(PointerMovedEvent, OnPointerMoved);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!IsDragging) return;
        
        RaiseEvent(new PortDragEventArgs(this, e) {RoutedEvent = PortDragEvent});
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        IsDragging = false;
        RaiseEvent(new PortDragEventArgs(this, e) {RoutedEvent = PortReleasedEvent});
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        IsDragging = true;
        RaiseEvent(new PortDragEventArgs(this, e) {RoutedEvent = PortPressedEvent});
    }

    public Point? GetCenterPoint(Visual relativeTo)
    {
        return PortHandler.TranslatePoint(default, relativeTo) +
                new Point(PortHandler.DesiredSize.Width / 2, PortHandler.DesiredSize.Height / 2);
    }
}

public class PortEventArgs : RoutedEventArgs
{
    public Port Port { get; }
    
    public PortEventArgs(Port port)
    {
        Port = port;
    }
}

public class PortDragEventArgs : PortEventArgs
{
    public PointerEventArgs PointerArgs { get; }
    
    public PortDragEventArgs(Port port, PointerEventArgs pointerArgs) : base(port)
    {
        PointerArgs = pointerArgs;
    }
}