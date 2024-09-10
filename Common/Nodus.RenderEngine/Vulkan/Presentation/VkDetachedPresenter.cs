using System.Reactive.Subjects;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Memory;
using Nodus.RenderEngine.Vulkan.Meta;
using Nodus.RenderEngine.Vulkan.Rendering;
using Nodus.RenderEngine.Vulkan.Sync;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Presentation;

public class VkDetachedPresenter : IVkRenderPresenter
{
    public IObservable<RenderPresentEvent> EventStream => eventSubject;

    private readonly IVkMemory imageMemory;
    private readonly IVkImage bufferImage;
    private readonly IVkFrameBuffer frameBuffer;
    private readonly IVkFence[] inFlightFences;
    private readonly IVkCommandPool commandPool;
    
    private readonly Subject<RenderPresentEvent> eventSubject;

    public VkDetachedPresenter(IVkContext context, IVkLogicalDevice device, IVkPhysicalDevice physicalDevice, IVkRenderSupplier renderSupplier,
        IVkRenderPass renderPass, VkQueueInfo queueInfo, uint maxConcurrentFrames)
    {
        eventSubject = new Subject<RenderPresentEvent>();
        commandPool = new VkCommandPool(context, device, queueInfo, 1, CommandPoolCreateFlags.ResetCommandBufferBit);
        imageMemory = new VkMemory(context, device, physicalDevice, MemoryPropertyFlags.DeviceLocalBit);
        
        var deviceProps = context.Api.GetPhysicalDeviceProperties(physicalDevice.WrappedDevice);
        bufferImage = new VkImage(context, device, new VkImageSpecification(ImageType.Type2D, 
            renderSupplier.CurrentRenderExtent.CastUp(), Format.B8G8R8A8Srgb, ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferSrcBit,
            ImageViewType.Type2D, deviceProps.Limits.MaxSamplerAnisotropy));
        
        imageMemory.AllocateForImage(context, bufferImage.WrappedImage, device);
        bufferImage.BindToMemory(imageMemory);
        bufferImage.CreateView();

        inFlightFences = new IVkFence[maxConcurrentFrames];
        for (var i = 0; i < maxConcurrentFrames; i++)
        {
            inFlightFences[i] = new VkFence(context, device, true);
        }

        frameBuffer = new VkFrameBuffer(context, device, renderPass, [bufferImage.Views[0]], renderSupplier);
    }
    
    public bool TryPrepareNewFrame(IVkSemaphore semaphore, uint frameIndex)
    {
        return true;
    }

    public void ProcessRenderQueue(Queue queue, IVkSemaphore semaphore, IVkFence fence)
    {
        fence.Await();
    }

    public Framebuffer GetAvailableFramebuffer()
    {
        return frameBuffer.WrappedBuffer;
    }

    public IVkFence GetPresentationFence(uint frameIndex)
    {
        return inFlightFences[frameIndex];
    }

    public void Dispose()
    {
        bufferImage.Dispose();
        imageMemory.Dispose();
        frameBuffer.Dispose();
        commandPool.Dispose();
        inFlightFences.DisposeAll();
        eventSubject.Dispose();
    }
}