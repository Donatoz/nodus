using System;
using Nodus.Core.ViewModels;
using Nodus.NodeEditor.Factories;
using Nodus.RenderEditor.ViewModels;
using Nodus.RenderEngine.Common;

namespace Nodus.RenderEditor.Factories;

public interface IRenderCanvasViewModelComponentFactory : INodeCanvasViewModelComponentFactory
{
    RenderPreviewViewModel CreateRenderPreview(IObservable<IRenderContext> contextStream);
    ContentBrowserViewModel CreateContentBrowser();
}

public class RenderCanvasViewModelComponentFactory : NodeCanvasViewModelComponentFactory,
    IRenderCanvasViewModelComponentFactory
{
    public RenderPreviewViewModel CreateRenderPreview(IObservable<IRenderContext> contextStream)
    {
        return new RenderPreviewViewModel(contextStream);
    }

    public ContentBrowserViewModel CreateContentBrowser()
    {
        return new ContentBrowserViewModel();
    }
}