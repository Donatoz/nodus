using System.Linq;
using Avalonia;
using FlowEditor.Meta;
using Nodus.Core.Controls;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Views;

namespace FlowEditor.Views;

public class FlowCanvas : NodeCanvas
{
    private DataContextControlBinding? dataContextBinding;
    
    public FlowCanvas(IComponentFactoryProvider<NodeCanvas> factoryProvider) : base(factoryProvider)
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
        var flowConnection = currentConnections.FirstOrDefault(x => x.ViewModel.TargetNode.NodeId == e.Node.NodeId
                                                                    && x.From is FlowPort p && p.ValueType == typeof(FlowType));
        
        if (flowConnection.ViewModel == null) return;
        
        flowConnection.Path.SetActive(e.IsResolved);
    }

    protected virtual FlowController CreateFlowControllerControl()
    {
        return new FlowController();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        dataContextBinding?.Dispose();
    }
}