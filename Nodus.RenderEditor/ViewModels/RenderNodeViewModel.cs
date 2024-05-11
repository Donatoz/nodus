using Nodus.DI.Factories;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using Nodus.RenderEditor.Models;

namespace Nodus.RenderEditor.ViewModels;

public class RenderNodeViewModel : NodeViewModel
{
    public RenderNodeViewModel(IRenderNodeModel model, 
        IFactoryProvider<NodeCanvasViewModel> componentFactoryProvider, 
        IFactoryProvider<INodeCanvasModel> modelFactoryProvider) 
        : base(model, componentFactoryProvider, modelFactoryProvider)
    {
    }
}