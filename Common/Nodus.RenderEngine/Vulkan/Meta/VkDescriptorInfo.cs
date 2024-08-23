using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Meta;

public readonly struct VkDescriptorInfo
{
    public DescriptorType Type { get; init; }
}