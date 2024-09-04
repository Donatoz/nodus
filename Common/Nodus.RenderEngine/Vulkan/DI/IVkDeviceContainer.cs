using Nodus.RenderEngine.Vulkan.Meta;

namespace Nodus.RenderEngine.Vulkan.DI;

/// <summary>
/// Represents a Vulkan physical & logical device pair.
/// </summary>
public interface IVkDeviceContainer
{
    IVkLogicalDevice LogicalDevice { get; }
    IVkPhysicalDevice PhysicalDevice { get; }
}

public sealed record VkDeviceContainer(
    IVkLogicalDevice LogicalDevice, 
    IVkPhysicalDevice PhysicalDevice)
    : IVkDeviceContainer;