using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Meta;

public interface IVkDescriptorRequest
{
    DescriptorType Type { get; }
    uint Count { get; }
    uint Binding { get; }
    ShaderStageFlags Stages { get; }
    VkDescriptorContent Content { get; }
}

public readonly struct VkDescriptorRequest(
    DescriptorType type, 
    uint count, 
    uint binding, 
    ShaderStageFlags stages, 
    VkDescriptorContent content = VkDescriptorContent.None)
    : IVkDescriptorRequest
{
    public DescriptorType Type { get; } = type;
    public uint Count { get; } = count;
    public uint Binding { get; } = binding;
    public ShaderStageFlags Stages { get; } = stages;
    public VkDescriptorContent Content { get; } = content;
}

public readonly struct VkPushConstantRequest(PushConstantRange range, VkPushConstantsContent content = VkPushConstantsContent.None)
{
    public PushConstantRange Range { get; } = range;
    public VkPushConstantsContent Content { get; } = content;
}