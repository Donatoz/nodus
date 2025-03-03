using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Meta;

public readonly struct VkRenderAttachmentsInfo
{
    public uint ColorAttachmentCount { get; }
    public Format[] ColorFormats { get; }
    public Format DepthFormat { get; }
    public Format StencilFormat { get; }
    
    public VkRenderAttachmentsInfo(uint colorAttachmentCount, Format[] colorFormats, Format depthFormat, Format stencilFormat)
    {
        ColorAttachmentCount = colorAttachmentCount;
        ColorFormats = colorFormats;
        DepthFormat = depthFormat;
        StencilFormat = stencilFormat;
    }
}