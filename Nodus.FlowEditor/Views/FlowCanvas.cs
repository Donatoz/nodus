using System.Linq;
using FlowEditor.Meta;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Views;

namespace FlowEditor.Views;

public class FlowCanvas : NodeCanvas
{
    public FlowCanvas(IComponentFactoryProvider<NodeCanvas> factoryProvider) : base(factoryProvider)
    {
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        AddHandler(FlowNode.FlowNodeResolveEvent, OnNodeResolved);
    }

    private void OnNodeResolved(object? sender, FlowNodeResolveEventArgs e)
    {
        var flowConnection = currentConnections.FirstOrDefault(x => x.ViewModel.SourceNode.NodeId == e.Node.NodeId
                                                                    && x.From is FlowPort p && p.ValueType == typeof(FlowType));
        
        if (flowConnection.ViewModel == null) return;
        
        flowConnection.Path.SetActive(e.IsResolved);
    }
}