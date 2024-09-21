using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.Vulkan.Memory;

namespace Nodus.RenderEngine.Vulkan.DI;

/// <summary>
/// Represents an aggregation of individual Vulkan render service components.
/// </summary>
public interface IVkRenderServiceContainer
{
    IVkMemoryLessor MemoryLessor { get; }
    IVkDeviceContainer Devices { get; }
    IRenderDispatcher Dispatcher { get; }
}

public record VkRenderServiceContainer : IVkRenderServiceContainer
{
    public required IVkMemoryLessor MemoryLessor { get; init; }
    public required IVkDeviceContainer Devices { get; init; }
    public required IRenderDispatcher Dispatcher { get; init; }
}