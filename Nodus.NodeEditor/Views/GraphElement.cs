using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Nodus.Core.Extensions;
using Nodus.Core.Selection;

namespace Nodus.NodeEditor.Views;

public abstract class GraphElement : UserControl
{
    public static readonly RoutedEvent<NodeEventArgs> ElementPressedEvent = 
        RoutedEvent.Register<Node, NodeEventArgs>(nameof(ElementPressedEvent), RoutingStrategies.Bubble);
    
    public SelectableComponent SelectionHandler { get; }
    
    protected abstract Control ContainerControl { get; }
    protected abstract Control BodyControl { get; }

    public GraphElement()
    {
        SelectionHandler = new SelectableComponent(this);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        BodyControl.AddHandler(PointerPressedEvent, OnPointerPressed);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Control c || !c.HasVisualAncestorOrSelf(ContainerControl)) return;
        
        RaiseEvent(new ElementEventArgs(this) {RoutedEvent = ElementPressedEvent});
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        SelectionHandler.Dispose();
    }
}

public class ElementEventArgs : RoutedEventArgs
{
    public GraphElement Element { get; }

    public ElementEventArgs(GraphElement element)
    {
        Element = element;
    }
}