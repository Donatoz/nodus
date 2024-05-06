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
    public static readonly RoutedEvent<ElementPressedEventArgs> ElementPressedEvent = 
        RoutedEvent.Register<GraphElement, ElementPressedEventArgs>(nameof(ElementPressedEvent), RoutingStrategies.Bubble);
    
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
        
        RaiseEvent(new ElementPressedEventArgs(this, e.KeyModifiers) {RoutedEvent = ElementPressedEvent});
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        SelectionHandler.Dispose();
    }

    public virtual void DestroySelf(Action onDestructionComplete)
    {
        onDestructionComplete.Invoke();
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

public class ElementPressedEventArgs : ElementEventArgs
{
    public KeyModifiers Modifiers { get; }
    
    public ElementPressedEventArgs(GraphElement element, KeyModifiers modifiers) : base(element)
    {
        Modifiers = modifiers;
    }
}