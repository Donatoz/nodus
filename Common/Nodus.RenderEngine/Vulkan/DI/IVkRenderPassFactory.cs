using Nodus.Common;
using Nodus.DI.Factories;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.DI;

public interface IVkRenderPassFactory
{
    AttachmentDescription[] CreateAttachments(Format colorFormat, Format depthFormat);
    AttachmentReference[] CreateAttachmentReferences();
    SubpassDescription[] CreateSubPasses(IVkSubPassScheme[] schemes);
    SubpassDependency[] CreateDependencies();
}

public interface IVkSubPassScheme
{
    PipelineBindPoint BindPoint { get; }
    IFixedEnumerable<AttachmentReference> Attachments { get; }
}

public readonly struct VkSubPassScheme(
    PipelineBindPoint bindPoint, 
    IFixedEnumerable<AttachmentReference> attachments) : IVkSubPassScheme
{
    public PipelineBindPoint BindPoint { get; } = bindPoint;
    public IFixedEnumerable<AttachmentReference> Attachments { get; } = attachments;
}

public class VkRenderPassFactory : IVkRenderPassFactory
{
    public IFactory<Format, AttachmentDescription[]>? DescriptionsFactory { get; set; }
    public IFactory<AttachmentReference[]>? AttachmentReferencesFactory { get; set; }
    public IFactory<IVkSubPassScheme[], SubpassDescription[]>? SubPassesFactory { get; set; }
    public IFactory<SubpassDependency[]>? DependenciesFactory { get; set; }
    
    public AttachmentDescription[] CreateAttachments(Format colorFormat, Format depthFormat)
    {
        return DescriptionsFactory?.Create(colorFormat) ??
        [
            new AttachmentDescription
            {
                Format = colorFormat,
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.PresentSrcKhr
            },
            new AttachmentDescription
            {
                Format = depthFormat,
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.DontCare,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.DepthStencilAttachmentOptimal
            }
        ];
    }

    public AttachmentReference[] CreateAttachmentReferences()
    {
        return AttachmentReferencesFactory?.Create() ?? [
            new AttachmentReference
            {
                Attachment = 0,
                Layout = ImageLayout.ColorAttachmentOptimal
            },
            new AttachmentReference
            {
                Attachment = 1,
                Layout = ImageLayout.DepthStencilAttachmentOptimal
            }
        ];
    }

    public unsafe SubpassDescription[] CreateSubPasses(IVkSubPassScheme[] schemes)
    {
        if (SubPassesFactory != null)
        {
            return SubPassesFactory.Create(schemes);
        }
        
        var descs = new SubpassDescription[schemes.Length];

        for (var i = 0; i < descs.Length; i++)
        {
            var scheme = schemes[i];

            descs[i] = new SubpassDescription
            {
                PipelineBindPoint = scheme.BindPoint,
                ColorAttachmentCount = 1,
                PColorAttachments = scheme.Attachments.Data,
                PDepthStencilAttachment = scheme.Attachments.Data + 1
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
                SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
                SrcAccessMask = 0,
                DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit | PipelineStageFlags.EarlyFragmentTestsBit,
                DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.DepthStencilAttachmentWriteBit
            }
        ];
    }
}