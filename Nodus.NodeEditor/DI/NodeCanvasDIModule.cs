using Ninject.Modules;
using Nodus.DI.Factories;
using Nodus.DI.Modules;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using Nodus.NodeEditor.Views;

namespace Nodus.NodeEditor.DI;

[ModuleInjectionEntry(typeof(NodeCanvasModel))]
public class NodeCanvasDIModule : NinjectModule
{
    public override void Load()
    {
        Rebind<IComponentFactoryProvider<INodeCanvasModel>>()
            .To<NodeCanvasComponentFactoryProvider>()
            .WhenInjectedInto<INodeCanvasModel>()
            .InTransientScope();
        
        Rebind<INodeCanvasViewModelComponentFactory>()
            .To<NodeCanvasViewModelComponentFactory>()
            .WhenInjectedInto<NodeCanvasViewModel>()
            .InTransientScope();

        Rebind<IComponentFactoryProvider<NodeCanvasViewModel>>()
            .To<NodeCanvasViewModelFactoryProvider>()
            .InTransientScope();

        Rebind<IComponentFactoryProvider<NodeCanvas>>()
            .To<NodeCanvasControlFactoryProvider>()
            .InTransientScope();

        Bind<INodeContextProvider>()
            .To<NodeContextProvider>()
            .InSingletonScope();
    }
}