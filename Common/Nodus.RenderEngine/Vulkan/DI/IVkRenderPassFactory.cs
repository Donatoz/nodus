using Nodus.DI.Factories;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.DI;

public interface IVkRenderPassFactory
{
    AttachmentDescription[] CreateAttachments(Format format);
    SubpassDescription[] CreateSubPasses(IVkSubPassScheme[] subPassSchemes);
    SubpassDependency[] CreateDependencies();
}

public interface IVkSubPassScheme
{
    PipelineBindPoint BindPoint { get; }
    uint ColorAttachmentsCount { get; }
    unsafe AttachmentReference* ColorAttachments { get; }
}

public readonly unsafe struct VkSubPassScheme(PipelineBindPoint bindPoint, uint colorAttachmentsCount, AttachmentReference* colorAttachments) : IVkSubPassScheme
{
    public PipelineBindPoint BindPoint { get; } = bindPoint;
    public uint ColorAttachmentsCount { get; } = colorAttachmentsCount;
    public AttachmentReference* ColorAttachments { get; } = colorAttachments;
}

public class VkRenderPassFactory : IVkRenderPassFactory
{
    public IFactory<Format, AttachmentDescription[]>? DescriptionsFactory { get; set; }
    public IFactory<IVkSubPassScheme[], SubpassDescription[]>? SubPassesFactory { get; set; }
    public IFactory<SubpassDependency[]>? DependenciesFactory { get; set; }
    
    public AttachmentDescription[] CreateAttachments(Format format)
    {
        return DescriptionsFactory?.Create(format) ??
        [
            new AttachmentDescription
            {
                Format = format,
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.PresentSrcKhr
            }
        ];
    }

    public unsafe SubpassDescription[] CreateSubPasses(IVkSubPassScheme[] subPassSchemes)
    {
        if (SubPassesFactory != null)
        {
            return SubPassesFactory.Create(subPassSchemes);
        }
        
        var descs = new SubpassDescription[subPassSchemes.Length];

        for (var i = 0; i < descs.Length; i++)
        {
            var scheme = subPassSchemes[i];

            descs[i] = new SubpassDescription
            {
                PipelineBindPoint = scheme.BindPoint,
                ColorAttachmentCount = scheme.ColorAttachmentsCount,
                PColorAttachments = scheme.ColorAttachments
            };
        }

        return descs;
    }

    public SubpassDependency[] CreateDependencies()
    {
        return DependenciesFactory?.Create() ?? 
        [
            new SubpassDependency
            {
                SrcSubpass = Vk.SubpassExternal,
                DstSubpass = 0,
                SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
                SrcAccessMask = 0,
                DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
                DstAccessMask = AccessFlags.ColorAttachmentWriteBit
            }
        ];
    }
}