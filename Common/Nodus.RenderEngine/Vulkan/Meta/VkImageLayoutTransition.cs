using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Meta;

public readonly struct VkImageLayoutTransition(ImageLayout from, ImageLayout to)
{
    public ImageLayout From { get; } = from;
    public ImageLayout To { get;} = to;
}