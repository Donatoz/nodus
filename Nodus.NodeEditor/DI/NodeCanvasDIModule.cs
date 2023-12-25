using Ninject.Modules;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.ViewModels;

namespace Nodus.NodeEditor.DI;

public class NodeCanvasDIModule : NinjectModule
{
    public override void Load()
    {
        Bind<INodeCanvasViewModelComponentFactory>().To<NodeCanvasViewModelComponentFactory>().InTransientScope();
    }
}