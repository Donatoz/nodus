using FlowEditor.Models;
using FlowEditor.ViewModels;
using Ninject.Parameters;
using Nodus.DI;
using Nodus.DI.Runtime;
using Nodus.FlowLibraries.Common;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using ReactiveUI;

namespace Nodus.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    public NodeCanvasViewModel CanvasViewModel { get; }
    
    public MainWindowViewModel(IRuntimeElementProvider elementProvider, IRuntimeModuleLoader moduleLoader)
    {
        moduleLoader.Repopulate();
        
        moduleLoader.LoadModulesForType<NodeCanvasModel>();
        moduleLoader.LoadModulesForType<FlowCanvasModel>();
        
        CommonFlowLibrary.Register(elementProvider.GetRuntimeElement<INodeContextProvider>(), elementProvider);
        
        CanvasViewModel = elementProvider.GetRuntimeElement<FlowCanvasViewModel>(
            new TypeMatchingConstructorArgument(typeof(IFlowCanvasModel), (_, _) => PrepareCanvas(elementProvider)));
    }

    private INodeCanvasModel PrepareCanvas(IRuntimeElementProvider elementProvider)
    {
        var canvas = elementProvider.GetRuntimeElement<FlowCanvasModel>();

        return canvas;
    }
}