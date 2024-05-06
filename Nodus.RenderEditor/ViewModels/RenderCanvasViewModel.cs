using System;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Factories;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace Nodus.RenderEditor.ViewModels;

public class RenderCanvasViewModel : NodeCanvasViewModel
{
    public RenderCanvasViewModel(INodeCanvasModel model, 
        IServiceProvider serviceProvider, 
        INodeCanvasViewModelComponentFactory componentFactory, 
        IFactory<IGraphElementModel, ElementViewModel> elementsFactory) : base(model, serviceProvider, componentFactory, elementsFactory)
    {
    }
}