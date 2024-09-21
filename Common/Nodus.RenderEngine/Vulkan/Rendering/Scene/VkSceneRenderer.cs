using Nodus.Core.Extensions;
using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.Vulkan.Sync;

namespace Nodus.RenderEngine.Vulkan.Rendering;

public interface IVkSceneRendererContext
{
    IRenderScene Scene { get; }
}

public class VkSceneRenderer : VkMultiBufferRenderer
{
    
    public VkSceneRenderer()
    {
        
    }

    protected override IEnumerable<IVkTask> GetRenderTasksForFrame(int frameIndex)
    {
        return base.GetRenderTasksForFrame(frameIndex);
    }
}