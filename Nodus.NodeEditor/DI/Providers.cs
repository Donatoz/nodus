using Nodus.Core.Factories;
using Nodus.DI;
using Nodus.DI.Factories;
using Nodus.DI.Runtime;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using Nodus.NodeEditor.Views;

namespace Nodus.NodeEditor.DI;

internal class NodeCanvasComponentFactoryProvider : TypedComponentFactoryProvider<INodeCanvasModel>
{
    public NodeCanvasComponentFactoryProvider(IRuntimeInjector injector)
    {
        RegisterFactory(typeof(INodeModelFactory), new NodeModelFactory(injector));
        RegisterFactory(typeof(IPortModelFactory), new PortModelFactory());
    }
}

internal class NodeCanvasViewModelFactoryProvider : TypedComponentFactoryProvider<NodeCanvasViewModel>
{
    public NodeCanvasViewModelFactoryProvider(IRuntimeElementProvider elementProvider)
    {
        RegisterFactory(typeof(INodeViewModelFactory), new NodeViewModelFactory(elementProvider));
        RegisterFactory(typeof(IPortViewModelFactory), new PortViewModelFactory(elementProvider));
    }
}

internal class NodeCanvasControlFactoryProvider : TypedComponentFactoryProvider<NodeCanvas>
{
    public NodeCanvasControlFactoryProvider()
    {
        this.RegisterControlFactory<Node, NodeViewModel>();
        this.RegisterControlFactory<ConnectionPath, ConnectionViewModel>();
    }
}