namespace Nodus.RenderEngine.Vulkan.Meta;

[Flags]
public enum VkDescriptorContent
{
    None,
    Transformations,
    PrimaryTextures
}

[Flags]
public enum VkPushConstantsContent
{
    None,
    FrameTime
}