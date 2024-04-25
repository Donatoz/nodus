using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.Templates;
using FlowEditor.Meta;
using FlowEditor.Views.Templates;
using Nodus.Core.Controls;
using Nodus.Core.Extensions;
using Nodus.Core.ViewModels;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.Views;

namespace FlowEditor.Views;

public class FlowCanvas : NodeCanvas
{
    private DataContextControlBinding? dataContextBinding;
    
    public FlowCanvas(IFactoryProvider<NodeCanvas> factoryProvider) : base(factoryProvider)
    {
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        var flowController = CreateFlowControllerControl();
        CanvasContainer.Children.Add(flowController);

        dataContextBinding = new DataContextControlBinding(this, (flowController, () => DataContext));
        
        AddHandler(FlowNode.FlowNodeResolveEvent, OnNodeResolved);
    }

    private void OnNodeResolved(object? sender, FlowNodeResolveEventArgs e)
    {
        if (e.SourceConnection == null) return;

        var flowConnection = currentConnections.FirstOrDefault(x => x.Data.Equals(e.SourceConnection));

        if (flowConnection == null)
        {
            throw new Exception($"Failed to find flow connection: {e.SourceConnection.Value.SourceNodeId}->{e.SourceConnection.Value.TargetNodeId}");
        }
        
        if (flowConnection.ViewModel == null) return;
        
        flowConnection.Path.SetActive(e.IsResolved);
    }

    protected virtual FlowController CreateFlowControllerControl()
    {
        return new FlowController();
    }

    protected override IDataTemplate CreateComponentSelector()
    {
        return new FlowCanvasComponentSelector();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        dataContextBinding?.Dispose();
    }
}