namespace Nodus.RenderEngine.Vulkan.Meta;

public readonly ref struct VkImageCopyRange(uint baseArrayLayer = 0, uint? layerCount = null, uint bufferOffset = 0)
{
    public uint BaseArrayLayer { get; } = baseArrayLayer;
    public uint? LayerCount { get; } = layerCount;
    public uint BufferOffset { get; } = bufferOffset;
}