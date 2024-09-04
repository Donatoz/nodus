using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Meta;

/// <summary>
/// Represents information about a Vulkan queue family.
/// </summary>
public struct VkQueueInfo
{
    /// <summary>
    /// Graphics queue family index.
    /// </summary>
    public uint? GraphicsFamily { get; private set; }

    /// <summary>
    /// Present queue family index.
    /// </summary>
    public uint? PresentFamily { get; private set; }
    
    public uint? ComputeFamily { get; private set; }

    /// <summary>
    /// Check whether the Vulkan queue family information is complete.
    /// </summary>
    /// <returns>
    /// True if the Vulkan queue family information is complete, otherwise false.
    /// </returns>
    public bool IsComplete()
    {
        return GraphicsFamily != null && PresentFamily != null && ComputeFamily != null;
    }

    /// <summary>
    /// Get queue family information from the phyiscal device.
    /// </summary>
    /// <param name="device">The physical device to get the queue family information from.</param>
    /// <param name="vk">The Vulkan API provider.</param>
    /// <param name="surface">The Vulkan surface.</param>
    public static unsafe VkQueueInfo GetFromDevice(PhysicalDevice device, Vk vk, IVkKhrSurface? surface = null)
    {
        var indices = new VkQueueInfo();

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

            if (queueFamily.QueueFlags.HasFlag(QueueFlags.ComputeBit))
            {
                indices.ComputeFamily = i;
            }

            if (surface != null)
            {
                surface.Extension.GetPhysicalDeviceSurfaceSupport(device, i, surface.SurfaceKhr, out var isPresentSupported);

                if (isPresentSupported)
                {
                    indices.PresentFamily = i;
                }
            }

            if (indices.IsComplete())
            {
                break;
            }

            i++;
        }

        return indices;
    }

    public IEnumerable<uint> GetFamilies()
    {
        if (GraphicsFamily != null)
        {
            yield return GraphicsFamily.Value;
        }
        
        if (PresentFamily != null)
        {
            yield return PresentFamily.Value;
        }

        if (ComputeFamily != null)
        {
            yield return ComputeFamily.Value;
        }
    }
}

public static class QueueInfoExtensions
{
    public static void ThrowIfIncomplete(this VkQueueInfo info)
    {
        if (!info.IsComplete())
        {
            throw new Exception("Queue families are not complete.");
        }
    }
}