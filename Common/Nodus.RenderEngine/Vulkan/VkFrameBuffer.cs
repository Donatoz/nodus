using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Rendering;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan;

public interface IVkFrameBuffer : IVkUnmanagedHook
{
    Framebuffer WrappedBuffer { get; }
}

public class VkFrameBuffer : VkObject, IVkFrameBuffer
{
    public Framebuffer WrappedBuffer { get; }

    private readonly IVkLogicalDevice device;
    
    public unsafe VkFrameBuffer(IVkContext vkContext, IVkLogicalDevice device, IVkRenderPass renderPass, ImageView view, IVkRenderSupplier renderSupplier) : base(vkContext)
    {
        this.device = device;
        var attachedView = view;

        var bufferCreateInfo = new FramebufferCreateInfo
        {
            SType = StructureType.FramebufferCreateInfo,
            RenderPass = renderPass.WrappedPass,
            AttachmentCount = 1,
            PAttachments = &attachedView,
            Width = renderSupplier.CurrentRenderExtent.Width,
            Height = renderSupplier.CurrentRenderExtent.Height,
            Layers = 1
        };

        Context.Api.CreateFramebuffer(device.WrappedDevice, &bufferCreateInfo, null, out var buffer)
            .TryThrow("Failed to create framebuffer.");

        WrappedBuffer = buffer;
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            Context.Api.DestroyFramebuffer(device.WrappedDevice, WrappedBuffer, null);
        }
        
        base.Dispose(disposing);
    }
}