using Nodus.Core.Factories;
using Nodus.DI.Runtime;
using Nodus.NodeEditor.DI;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.ViewModels;
using Nodus.NodeEditor.Views;
using Nodus.RenderEditor.Factories;
using Nodus.RenderEditor.Views;

namespace Nodus.RenderEditor.DI;

public class RenderCanvasModelFactoryProvider : NodeCanvasComponentFactoryProvider
{
    public RenderCanvasModelFactoryProvider()
    {
        RegisterFactory(typeof(INodeModelFactory), new RenderNodeModelFactory());
        RegisterFactory(typeof(IPortModelFactory), new RenderPortModelFactory());
    }
}

public class RenderCanvasViewModelFactoryProvider : NodeCanvasViewModelFactoryProvider
{
    public RenderCanvasViewModelFactoryProvider(IRuntimeElementProvider elementProvider) : base(elementProvider)
    {
        RegisterFactory(typeof(INodeViewModelFactory), new RenderNodeViewModelFactory(elementProvider));
        RegisterFactory(typeof(IPortViewModelFactory), new RenderPortViewModelFactory(elementProvider));
    }
}

public class RenderCanvasControlFactoryProvider : NodeCanvasControlFactoryProvider
{
    public RenderCanvasControlFactoryProvider()
    {
        RegisterFactory(typeof(IControlFactory<Node, NodeViewModel>), new ControlFactory<RenderNode, NodeViewModel>());
    }
}