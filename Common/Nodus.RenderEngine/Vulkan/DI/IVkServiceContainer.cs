using Nodus.RenderEngine.Vulkan.Memory;

namespace Nodus.RenderEngine.Vulkan.DI;

public interface IVkServiceContainer
{
    IVkMemoryLessor MemoryLessor { get; }
}

public record VkServiceContainer : IVkServiceContainer
{
    public required IVkMemoryLessor MemoryLessor { get; init; }
}