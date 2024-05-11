using System;
using System.Reactive.Linq;
using Nodus.Core.Entities;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using Nodus.RenderEditor.Factories;

namespace Nodus.RenderEditor.ViewModels;

public class RenderCanvasViewModel : NodeCanvasViewModel
{
    public RenderCanvasViewModel(INodeCanvasModel model, 
        IServiceProvider serviceProvider, 
        IRenderCanvasViewModelComponentFactory componentFactory, 
        IFactory<IGraphElementModel, ElementViewModel> elementsFactory) : base(model, serviceProvider, componentFactory, elementsFactory)
    {
        this.AttachContainer(componentFactory.CreateContentBrowser());
        
        var renderPreview = componentFactory.CreateRenderPreview();
        
        renderPreview.AttachDisposable(renderPreview.EventStream.OfType<RenderPreviewViewModel.UpdatePreviewEvent>().Subscribe(OnUpdatePreview));
        this.AttachDisposableContainer(renderPreview);
    }

    private void OnUpdatePreview(RenderPreviewViewModel.UpdatePreviewEvent evt)
    {
        
    }
}