using Nodus.Common;
using Nodus.RenderEngine.Vulkan.Meta;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.DI;

public interface IVkPipelineDecorator
{
    Chain ExtendChain(Chain chain);
}

public class VkRenderAttachmentsInjector : IVkPipelineDecorator, IDisposable
{
    private readonly VkRenderAttachmentsInfo info;
    private readonly FixedArray<Format> formats;
    
    public VkRenderAttachmentsInjector(VkRenderAttachmentsInfo info)
    {
        this.info = info;
        formats = info.ColorFormats.ToFixedArray();
    }
    
    public unsafe Chain ExtendChain(Chain chain)
    {
        return chain.AddAny(new PipelineRenderingCreateInfo
        {
            SType = StructureType.PipelineRenderingCreateInfo,
            ColorAttachmentCount = info.ColorAttachmentCount,
            PColorAttachmentFormats = formats.Data,
            DepthAttachmentFormat = info.DepthFormat,
            StencilAttachmentFormat = info.StencilFormat
        });
    }

    public void Dispose()
    {
        formats.Dispose();
    }
}