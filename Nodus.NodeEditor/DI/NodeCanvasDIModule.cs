using Ninject.Modules;
using Nodus.Core.ViewModels;
using Nodus.DI.Factories;
using Nodus.DI.Modules;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using Nodus.NodeEditor.Views;

namespace Nodus.NodeEditor.DI;

[ModuleInjectionEntry(typeof(NodeCanvasModel))]
public class NodeCanvasDIModule : NinjectModule
{
    public override void Load()
    {
        // Factory providers
        
        Rebind<IFactoryProvider<INodeCanvasModel>>()
            .To<NodeCanvasComponentFactoryProvider>()
            .InTransientScope();

        Rebind<IFactoryProvider<NodeCanvasViewModel>>()
            .To<NodeCanvasViewModelFactoryProvider>()
            .InTransientScope();

        Rebind<IFactoryProvider<NodeCanvas>>()
            .To<NodeCanvasControlFactoryProvider>()
            .InTransientScope();
        
        // Data -> Template -> Model -> ViewModel pipeline
        
        Rebind<IFactory<IGraphElementData, IGraphElementTemplate>>()
            .To<ElementTemplateFactory>()
            .InSingletonScope();

        Rebind<IFactory<IGraphElementTemplate, IGraphElementModel>>()
            .To<ElementFactory>()
            .InSingletonScope();

        Rebind<IFactory<IGraphElementModel, ElementViewModel>>()
            .To<ElementViewModelFactory>()
            .InSingletonScope();

        // Specialized factories
        
        Rebind<INodeCanvasViewModelComponentFactory>()
            .To<NodeCanvasViewModelComponentFactory>()
            .WhenInjectedInto<NodeCanvasViewModel>()
            .InTransientScope();
        
        // Miscellaneous providers
        
        Bind<INodeContextProvider>()
            .To<NodeContextProvider>()
            .InSingletonScope();
    }
}