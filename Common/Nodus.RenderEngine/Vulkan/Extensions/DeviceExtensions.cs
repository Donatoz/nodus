using Nodus.RenderEngine.Vulkan.Meta;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Extensions;

public static class DeviceExtensions
{
    public static Queue? TryGetGraphicsQueue(this IVkLogicalDevice device, VkQueueInfo info)
    {
        return info.GraphicsFamily != null
            ? device.Queues.TryGetValue(info.GraphicsFamily.Value, out var family)
                ? family
                : null
            : null;
    }
}