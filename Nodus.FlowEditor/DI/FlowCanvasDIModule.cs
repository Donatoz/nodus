using FlowEditor.Models;
using Ninject.Modules;
using Nodus.DI.Factories;
using Nodus.DI.Modules;
using Nodus.FlowEngine;
using Nodus.NodeEditor.DI;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using Nodus.NodeEditor.Views;

namespace FlowEditor.DI;

[ModuleInjectionEntry(typeof(FlowCanvasModel))]
public class FlowCanvasDIModule : NinjectModule
{
    public override void Load()
    {
        Rebind<IComponentFactoryProvider<INodeCanvasModel>>()
            .To<FlowCanvasModelFactoryProvider>()
            .InTransientScope();
        
        Rebind<IComponentFactoryProvider<NodeCanvasViewModel>>()
            .To<FlowCanvasViewModelFactoryProvider>()
            .InTransientScope();

        Rebind<IComponentFactoryProvider<NodeCanvas>>()
            .To<FlowCanvasControlFactoryProvider>()
            .InTransientScope();

        Bind<IFlowProducer>()
            .To<SingleThreadProducer>()
            .InSingletonScope();

        Bind<IGraphFlowBuilder>()
            .To<GraphFlowBuilder>()
            .InSingletonScope();
    }
}