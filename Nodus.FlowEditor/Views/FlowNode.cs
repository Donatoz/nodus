using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Threading;
using FlowEditor.Models;
using FlowEditor.ViewModels;
using FlowEditor.Views.Extensions;
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
    private FlowNodeResolveEffect? resolveEffect;
    
    protected override void OnInitialized()
    {
        base.OnInitialized();

        resolveEffect = new FlowNodeResolveEffect
        {
            ClipToBounds = false
        };
        NodeContainer.Children.Insert(0, resolveEffect);

        Menu.Items.Add(new MenuItem
        {
            Header = "Run Flow From This",
            Command = ReactiveCommand.Create(OnRunFlow)
        });
        Menu.Items.Add(new MenuItem
        {
            Header = "Extend",
            Command = ReactiveCommand.Create(OnExtend)
        });

        var extContainer = new ContextExtensionsContainer
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 5, 0, 0)
        };

        BottomExtensions.Children.Add(extContainer);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        
        resolveContract?.Dispose();

        if (DataContext is FlowNodeViewModel vm)
        {
            resolveContract = vm.CurrentResolveContext.AlterationStream.Subscribe(OnResolved);
        }
    }

    private void OnResolved(IFlowResolveContext? ctx)
    {
        // Flow context resolve happens most probably on different thread rather than UI thread.
        Dispatcher.UIThread.Invoke(() =>
        {
            resolveEffect?.SwitchState(ctx?.IsResolved ?? false);
            
            if (ctx == null) return;
            
            RaiseEvent(new FlowNodeResolveEventArgs(this, ctx.IsResolved, ctx.SourceConnection) 
                {RoutedEvent = FlowNodeResolveEvent});
        });
    }

    private void OnRunFlow()
    {
        if (DataContext is FlowNodeViewModel vm)
        {
            vm.RunFlow();
        }
    }

    private void OnExtend()
    {
        if (DataContext is FlowNodeViewModel vm)
        {
            vm.AddExtension();
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
    public Connection? SourceConnection { get; }

    public FlowNodeResolveEventArgs(FlowNode node, bool isResolved, Connection? connection) : base(node)
    {
        IsResolved = isResolved;
        SourceConnection = connection;
    }
}