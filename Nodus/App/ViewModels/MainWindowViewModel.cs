using FlowEditor.Models;
using FlowEditor.ViewModels;
using Ninject.Parameters;
using Nodus.DI;
using Nodus.DI.Runtime;
using Nodus.FlowLibraries.Common;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using Nodus.RenderEditor.Models;
using Nodus.RenderEditor.ViewModels;
using ReactiveUI;

namespace Nodus.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    public NodeCanvasViewModel CanvasViewModel { get; private set; }
    
    public MainWindowViewModel(IRuntimeElementProvider elementProvider, IRuntimeModuleLoader moduleLoader)
    {
        moduleLoader.Repopulate();
        
        LoadRenderGraph(elementProvider, moduleLoader);
    }

    private void LoadRenderGraph(IRuntimeElementProvider elementProvider, IRuntimeModuleLoader moduleLoader)
    {
        moduleLoader.LoadModulesForType<NodeCanvasModel>();

        CanvasViewModel = elementProvider.GetRuntimeElement<RenderCanvasViewModel>(
            new TypeMatchingConstructorArgument(typeof(INodeCanvasModel),
                (_, _) => PrepareCanvas<RenderCanvasModel>(elementProvider)));
    }

    private void LoadFlowGraph(IRuntimeElementProvider elementProvider, IRuntimeModuleLoader moduleLoader)
    {
        moduleLoader.LoadModulesForType<NodeCanvasModel>();
        moduleLoader.LoadModulesForType<FlowCanvasModel>();
        
        CommonFlowLibrary.Register(elementProvider.GetRuntimeElement<INodeContextProvider>(), elementProvider);
        
        CanvasViewModel = elementProvider.GetRuntimeElement<FlowCanvasViewModel>(
            new TypeMatchingConstructorArgument(typeof(IFlowCanvasModel), 
                (_, _) => PrepareCanvas<FlowCanvasModel>(elementProvider)));
    }

    private INodeCanvasModel PrepareCanvas<T>(IRuntimeElementProvider elementProvider) where T : INodeCanvasModel
    {
        var canvas = elementProvider.GetRuntimeElement<T>();

        return canvas;
    }
}