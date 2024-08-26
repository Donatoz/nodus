using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Extensions;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Rendering;

public interface IVkRenderPassContext
{
    Format Format { get; }
    
    IVkRenderPassFactory? Factory { get; }
}

public readonly struct VkRenderPassContext(Format format, IVkRenderPassFactory? factory = null) : IVkRenderPassContext
{
    public Format Format { get; } = format;
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
        var passFactory = context.Factory ?? new VkRenderPassFactory();
        
        var attachments = passFactory.CreateAttachments(context.Format);
        var colorAttachmentRef = new AttachmentReference
        {
            Attachment = 0,
            Layout = ImageLayout.ColorAttachmentOptimal
        };
        var subPasses = passFactory.CreateSubPasses([
            new VkSubPassScheme(PipelineBindPoint.Graphics, 1, &colorAttachmentRef)
        ]);
        var dependencies = passFactory.CreateDependencies();

        RenderPass pass;

        fixed (void* pAttachments = attachments, pSubPasses = subPasses, pDeps = dependencies)
        {
            var renderPass = new RenderPassCreateInfo
            {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = (uint)attachments.Length,
                PAttachments = (AttachmentDescription*)pAttachments,
                SubpassCount = (uint)subPasses.Length,
                PSubpasses = (SubpassDescription*)pSubPasses,
                DependencyCount = (uint)dependencies.Length,
                PDependencies = (SubpassDependency*)pDeps
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