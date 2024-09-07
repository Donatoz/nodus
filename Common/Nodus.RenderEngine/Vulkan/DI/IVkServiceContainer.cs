using Nodus.RenderEngine.Vulkan.Memory;

namespace Nodus.RenderEngine.Vulkan.DI;

/// <summary>
/// Represents an aggregation of individual Vulkan service components.
/// </summary>
public interface IVkServiceContainer
{
    IVkMemoryLessor MemoryLessor { get; }
    IVkDeviceContainer Devices { get; }
    
    // TODO: Add logger factory (Serilog)
}

public record VkServiceContainer : IVkServiceContainer
{
    public required IVkMemoryLessor MemoryLessor { get; init; }
    public required IVkDeviceContainer Devices { get; init; }
}