using System;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Nodus.Core.Extensions;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace Nodus.NodeEditor.Views;

public abstract class Port : UserControl
{
    public static readonly RoutedEvent<PortDragEventArgs> PortPressedEvent;
    public static readonly RoutedEvent<PortEventArgs> PortReleasedEvent;
    public static readonly RoutedEvent<PortDragEventArgs> PortDragEvent;
    
    protected bool IsDragging { get; private set; }
    
    public abstract Border PortHandler { get; }
    public string PortId { get; private set; }
    public virtual Color HandlerColor => PortHandler.BorderBrush is ImmutableSolidColorBrush s ? s.Color : Colors.Black;
    protected virtual string PortTypeClassPrefix => "port-type-";

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
            PortHandler.Classes.Where(x => x.StartsWith(PortTypeClassPrefix))
                .ForEach(x => PortHandler.Classes.Remove(x));
            PortHandler.Classes.Add(PortTypeClassPrefix + (vm.Type == PortType.Input ? "input" : "output"));
        }
    }

    protected override void OnInitialized()
    {
        PortHandler.AddHandler(PointerPressedEvent, OnPointerPressed);
        PortHandler.AddHandler(PointerReleasedEvent, OnPointerReleased);
        PortHandler.AddHandler(PointerMovedEvent, OnPointerMoved);
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

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (DataContext is PortViewModel vm)
        {
            var offset = PortHandler.DesiredSize.Width / -2;
            Margin = new Thickness(vm.Type == PortType.Input ? offset : 0, 10, 
                vm.Type == PortType.Output ? offset : 0, 10);
            HorizontalAlignment = vm.Type == PortType.Input ? HorizontalAlignment.Left : HorizontalAlignment.Right;
        }
        
        return base.ArrangeOverride(finalSize);
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