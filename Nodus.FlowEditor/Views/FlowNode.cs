using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FlowEditor.ViewModels;
using Nodus.Core.Extensions;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using Nodus.NodeEditor.Views;
using ReactiveUI;

namespace FlowEditor.Views;

public class FlowNode : Node
{
    public static readonly RoutedEvent<FlowNodeResolveEventArgs> FlowNodeResolveEvent = 
        RoutedEvent.Register<FlowNode, FlowNodeResolveEventArgs>(nameof(FlowNodeResolveEvent), RoutingStrategies.Bubble);
    
    private IDisposable? resolveContract;
    private Control resolveEffect;
    
    protected override void OnInitialized()
    {
        base.OnInitialized();

        resolveEffect = new Border
        {
            ClipToBounds = false
        };
        resolveEffect.Classes.Add("flow-node-resolve-effect");
        resolveEffect.Classes.Add("invisible");
        NodeContainer.Children.Insert(0, resolveEffect);

        Menu.Items.Add(new MenuItem
        {
            Header = "Run Flow From This",
            Command = ReactiveCommand.Create(OnRunFlow)
        });
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        resolveContract?.Dispose();

        if (DataContext is FlowNodeViewModel vm)
        {
            resolveContract = vm.IsBeingResolved.AlterationStream.Subscribe(OnResolved);
        }
    }

    private void OnResolved(bool isResolved)
    {
        // Flow context resolve happens most probably on different thread rather than UI thread.
        Dispatcher.UIThread.Invoke(() =>
        {
            resolveEffect.SwitchBetweenClasses("visible", "invisible", isResolved);
            
            RaiseEvent(new FlowNodeResolveEventArgs(this, isResolved) {RoutedEvent = FlowNodeResolveEvent});
        });
    }

    private void OnRunFlow()
    {
        if (DataContext is FlowNodeViewModel vm)
        {
            vm.RunFlow();
        }
    }

    protected override Port? CreatePortControl(PortViewModel vm)
    {
        return vm.Type switch
        {
            PortType.Input => new FlowInputPort { DataContext = vm },
            PortType.Output => new FlowOutputPort { DataContext = vm },
            _ => null
        };
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        resolveContract?.Dispose();
    }
}



public class FlowNodeResolveEventArgs : NodeEventArgs
{
    public bool IsResolved { get; }

    public FlowNodeResolveEventArgs(FlowNode node, bool isResolved) : base(node)
    {
        IsResolved = isResolved;
    }
}