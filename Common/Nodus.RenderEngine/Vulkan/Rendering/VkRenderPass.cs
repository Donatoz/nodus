using Nodus.Common;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Extensions;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Rendering;

public interface IVkRenderPassContext
{
    Format ColorFormat { get; }
    Format DepthFormat { get; }
    
    IVkRenderPassFactory? Factory { get; }
}

public readonly struct VkRenderPassContext(Format format, Format depthFormat, IVkRenderPassFactory? factory = null) : IVkRenderPassContext
{
    public Format ColorFormat { get; } = format;
    public Format DepthFormat { get; } = depthFormat;
    public IVkRenderPassFactory? Factory { get; } = factory;
}

public interface IVkRenderPass : IVkUnmanagedHook
{
    RenderPass WrappedPass { get; }
}

public class VkRenderPass : VkObject, IVkRenderPass
{
    public RenderPass WrappedPass { get; }

    private readonly IVkLogicalDevice device;
    
    public unsafe VkRenderPass(IVkContext vkContext, IVkLogicalDevice device, IVkRenderPassContext context) : base(vkContext)
    {
        this.device = device;
        var passFactory = context.Factory ?? VkRenderPassFactory.DefaultRenderPassFactory;
        
        var attachments = passFactory.CreateAttachments(context.ColorFormat, context.DepthFormat);
        using var attachmentRefs = passFactory.CreateAttachmentReferences().ToFixedArray();
        
        var subPasses = passFactory.CreateSubPasses([
            new VkSubPassScheme(PipelineBindPoint.Graphics, attachmentRefs)
        ]);
        var dependencies = passFactory.CreateDependencies();

        RenderPass pass;

        fixed (AttachmentDescription* pAttachments = attachments)
        fixed (SubpassDescription* pSubPasses = subPasses)
        fixed (SubpassDependency* pDeps = dependencies)
        {
            var renderPass = new RenderPassCreateInfo
            {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = (uint)attachments.Length,
                PAttachments = pAttachments,
                SubpassCount = (uint)subPasses.Length,
                PSubpasses = pSubPasses,
                DependencyCount = (uint)dependencies.Length,
                PDependencies = pDeps
            };
            
            Context.Api.CreateRenderPass(device.WrappedDevice, in renderPass, null, &pass)
                .TryThrow("Failed to create render pass.");
        }

        WrappedPass = pass;
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            Context.Api.DestroyRenderPass(device.WrappedDevice, WrappedPass, null);
        }
        
        base.Dispose(disposing);
    }
}