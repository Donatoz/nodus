using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan;

public struct VkQueueFamily
{
    public uint? GraphicsFamily { get; private set; }
    
    public bool IsComplete()
    {
        return GraphicsFamily.HasValue;
    }

    public static unsafe VkQueueFamily GetFromDevice(PhysicalDevice device, Vk vk)
    {
        var indices = new VkQueueFamily();

        var queueFamilyCount = 0u;
        vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilyCount, null);

        var queueFamilies = new QueueFamilyProperties[queueFamilyCount];
        
        fixed (QueueFamilyProperties* queueFamiliesPtr = queueFamilies)
        {
            vk.GetPhysicalDeviceQueueFamilyProperties(device, ref queueFamilyCount, queueFamiliesPtr);
        }
        
        var i = 0u;
        foreach (var queueFamily in queueFamilies)
        {
            if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
            {
                indices.GraphicsFamily = i;
            }

            if (indices.IsComplete())
            {
                break;
            }

            i++;
        }

        return indices;
    }
}