using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Meta;

public readonly struct VkDescriptorInfo
{
    public required DescriptorType Type { get; init; }
    public required uint Count { get; init; }
}