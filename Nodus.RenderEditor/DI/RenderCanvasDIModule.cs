using Ninject.Modules;
using Nodus.DI.Factories;
using Nodus.DI.Modules;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using Nodus.NodeEditor.Views;
using Nodus.RenderEditor.Factories;
using Nodus.RenderEditor.Models;

namespace Nodus.RenderEditor.DI;

[ModuleInjectionEntry(typeof(RenderCanvasModel))]
public class RenderCanvasDIModule : NinjectModule
{
    public override void Load()
    {
        Rebind<IRenderCanvasViewModelComponentFactory>()
            .To<RenderCanvasViewModelComponentFactory>()
            .InSingletonScope();
        
        Rebind<IFactoryProvider<NodeCanvasViewModel>>()
            .To<RenderCanvasViewModelFactoryProvider>()
            .InTransientScope();
        
        Rebind<IFactoryProvider<INodeCanvasModel>>()
            .To<RenderCanvasModelFactoryProvider>()
            .InTransientScope();
        
        Rebind<IFactoryProvider<NodeCanvas>>()
            .To<RenderCanvasControlFactoryProvider>()
            .InTransientScope();
    }
}