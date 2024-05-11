using Nodus.Core.ViewModels;
using Nodus.NodeEditor.Factories;
using Nodus.RenderEditor.ViewModels;

namespace Nodus.RenderEditor.Factories;

public interface IRenderCanvasViewModelComponentFactory : INodeCanvasViewModelComponentFactory
{
    RenderPreviewViewModel CreateRenderPreview();
    ContentBrowserViewModel CreateContentBrowser();
}

public class RenderCanvasViewModelComponentFactory : NodeCanvasViewModelComponentFactory,
    IRenderCanvasViewModelComponentFactory
{
    public RenderPreviewViewModel CreateRenderPreview()
    {
        return new RenderPreviewViewModel();
    }

    public ContentBrowserViewModel CreateContentBrowser()
    {
        return new ContentBrowserViewModel();
    }
}