using FlowEditor.Factories;
using FlowEditor.Models;
using Ninject.Modules;
using Nodus.DI.Factories;
using Nodus.DI.Modules;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using Nodus.NodeEditor.Views;
using Serilog;

namespace FlowEditor.DI;

/// <summary>
/// A Ninject module that contains default bindings with FlowCanvas minimal functionality.
/// </summary>
/// <remarks>
/// This module overrides <see cref="IComponentFactoryProvider"/>'s for <see cref="INodeCanvasModel"/>,
/// <see cref="NodeCanvasViewModel"/> and <see cref="NodeCanvas"/>.
/// </remarks>
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

        Rebind<INodeCanvasViewModelComponentFactory>()
            .To<FlowCanvasViewModelComponentFactory>()
            .InSingletonScope();

        Bind<IFlowLogger>()
            .ToConstant(new FlowLoggerWrapper(new LoggerConfiguration().WriteTo.Trace().MinimumLevel.Debug().CreateLogger()))
            .InSingletonScope();

        Bind<IFlowProducer>()
            .To<SingleThreadProducer>()
            .InSingletonScope();

        Bind<IGraphFlowBuilder>()
            .To<GraphFlowBuilder>()
            .InSingletonScope();
    }
}