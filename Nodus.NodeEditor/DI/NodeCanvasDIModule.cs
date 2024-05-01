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
        Rebind<IFactoryProvider<INodeCanvasModel>>()
            .To<NodeCanvasComponentFactoryProvider>()
            .WhenInjectedInto<INodeCanvasModel>()
            .InTransientScope();
        
        Rebind<INodeCanvasViewModelComponentFactory>()
            .To<NodeCanvasViewModelComponentFactory>()
            .WhenInjectedInto<NodeCanvasViewModel>()
            .InTransientScope();

        Rebind<IFactoryProvider<NodeCanvasViewModel>>()
            .To<NodeCanvasViewModelFactoryProvider>()
            .InTransientScope();

        Rebind<IFactoryProvider<NodeCanvas>>()
            .To<NodeCanvasControlFactoryProvider>()
            .InTransientScope();
        
        Rebind<IFactory<IGraphElementData, IGraphElementTemplate>>()
            .To<ElementTemplateFactory>()
            .InSingletonScope();

        Rebind<IFactory<IGraphElementTemplate, IGraphElementModel>>()
            .To<ElementFactory>()
            .InSingletonScope();

        Rebind<IFactory<IGraphElementModel, ElementViewModel>>()
            .To<ElementViewModelFactory>()
            .InSingletonScope();

        Bind<INodeContextProvider>()
            .To<NodeContextProvider>()
            .InSingletonScope();
    }
}