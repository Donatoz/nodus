using FlowEditor.Factories;
using FlowEditor.Models;
using FlowEditor.Views;
using Nodus.Core.Factories;
using Nodus.DI;
using Nodus.DI.Factories;
using Nodus.DI.Runtime;
using Nodus.NodeEditor.DI;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using Nodus.NodeEditor.Views;

namespace FlowEditor.DI;

public class FlowCanvasModelFactoryProvider : NodeCanvasComponentFactoryProvider
{
    public FlowCanvasModelFactoryProvider()
    {
        RegisterFactory(typeof(INodeModelFactory), new FlowNodeModelFactory());
        RegisterFactory(typeof(IPortModelFactory), new FlowPortModelFactory());
    }
}

public class FlowCanvasViewModelFactoryProvider : NodeCanvasViewModelFactoryProvider
{
    public FlowCanvasViewModelFactoryProvider(IRuntimeElementProvider elementProvider) : base(elementProvider)
    {
        RegisterFactory(typeof(INodeViewModelFactory), new FlowNodeViewModelFactory(elementProvider));
        RegisterFactory(typeof(IPortViewModelFactory), new FlowPortViewModelFactory(elementProvider));
    }
}

public class FlowCanvasControlFactoryProvider : NodeCanvasControlFactoryProvider
{
    public FlowCanvasControlFactoryProvider()
    {
        RegisterFactory(typeof(IControlFactory<Node, NodeViewModel>), new ControlFactory<FlowNode, NodeViewModel>());
    }
}