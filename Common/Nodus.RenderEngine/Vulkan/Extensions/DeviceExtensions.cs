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
    
    public static Queue RequireGraphicsQueue(this IVkLogicalDevice device, VkQueueInfo info)
    {
        if (info.GraphicsFamily == null)
        {
            throw new VulkanException("Graphics family was not specified in the provided queue info.");
        }

        if (!device.Queues.TryGetValue(info.GraphicsFamily.Value, out var family))
        {
            throw new VulkanException("Graphics queue is not present on the device.");
        }

        return family;
    }
}