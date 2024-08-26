using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Extensions;

public static class VulkanObjectExtensions
{
    public static Extent3D CastUp(this Extent2D extent, uint depth = 1)
    {
        return new Extent3D(extent.Width, extent.Height, depth);
    }
}