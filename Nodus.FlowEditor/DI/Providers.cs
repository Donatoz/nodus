using FlowEditor.Factories;
using FlowEditor.Models;
using FlowEditor.Views;
using Nodus.Core.Factories;
using Nodus.DI;
using Nodus.DI.Factories;
using Nodus.DI.Runtime;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using Nodus.NodeEditor.Views;

namespace FlowEditor.DI;

internal class FlowCanvasModelFactoryProvider : TypedComponentFactoryProvider<INodeCanvasModel>
{
    public FlowCanvasModelFactoryProvider()
    {
        RegisterFactory(typeof(INodeModelFactory), new FlowNodeModelFactory());
        RegisterFactory(typeof(IPortModelFactory), new FlowPortModelFactory());
    }
}

internal class FlowCanvasViewModelFactoryProvider : TypedComponentFactoryProvider<NodeCanvasViewModel>
{
    public FlowCanvasViewModelFactoryProvider(IRuntimeElementProvider elementProvider)
    {
        RegisterFactory(typeof(INodeViewModelFactory), new FlowNodeViewModelFactory(elementProvider));
        RegisterFactory(typeof(IPortViewModelFactory), new FlowPortViewModelFactory(elementProvider));
    }
}

internal class FlowCanvasControlFactoryProvider : TypedComponentFactoryProvider<NodeCanvas>
{
    public FlowCanvasControlFactoryProvider()
    {
        RegisterFactory(typeof(IControlFactory<Node, NodeViewModel>), new ControlFactory<FlowNode, NodeViewModel>());
        this.RegisterControlFactory<ConnectionPath, ConnectionViewModel>();
    }
}

internal class FlowCanvasFlowFactoryProvider : TypedComponentFactoryProvider<IFlowCanvasModel>
{
    public FlowCanvasFlowFactoryProvider()
    {
        RegisterFactory(typeof(IFlowProducerFactory), new FlowProducerFactory());
    }
}