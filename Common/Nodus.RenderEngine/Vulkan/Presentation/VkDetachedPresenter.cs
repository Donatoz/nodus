using System.Reactive.Subjects;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Vulkan.Convention;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Memory;
using Nodus.RenderEngine.Vulkan.Meta;
using Nodus.RenderEngine.Vulkan.Rendering;
using Nodus.RenderEngine.Vulkan.Sync;
using Nodus.RenderEngine.Vulkan.Utility;
using Serilog;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Presentation;

public class VkDetachedPresenter : IVkRenderPresenter
{
    public IObservable<RenderPresentEvent> EventStream => eventSubject;

    private readonly IVkMemoryLease imageMemory;
    private readonly IVkMemoryLease depthImageMemory;
    
    private readonly IVkImage bufferImage;
    private readonly IVkImage depthImage;
    
    private readonly IVkFrameBuffer frameBuffer;
    private readonly IVkFence[] inFlightFences;
    private readonly IVkCommandPool commandPool;

    private readonly ILogger logger;
    private readonly Subject<RenderPresentEvent> eventSubject;

    private DateTime lastPrepTime;

    public VkDetachedPresenter(IVkContext context, IVkRenderSupplier renderSupplier,
        IVkRenderPass renderPass, VkQueueInfo queueInfo, uint maxConcurrentFrames)
    {
        var device = context.RenderServices.Devices.LogicalDevice;
        var physicalDevice = context.RenderServices.Devices.PhysicalDevice;
        
        eventSubject = new Subject<RenderPresentEvent>();
        commandPool = new VkCommandPool(context, device, queueInfo, 1, CommandPoolCreateFlags.ResetCommandBufferBit);
        
        
        var deviceProps = context.Api.GetPhysicalDeviceProperties(physicalDevice.WrappedDevice);
        bufferImage = new VkImage(context, device, new VkImageSpecification(ImageType.Type2D, 
            renderSupplier.CurrentRenderExtent.CastUp(), Format.B8G8R8A8Srgb, ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferSrcBit,
            ImageViewType.Type2D, deviceProps.Limits.MaxSamplerAnisotropy));

        var depthFormat = ImageUtility.GetSupportedFormat(
            [Format.D32SfloatS8Uint, Format.D24UnormS8Uint],
            physicalDevice, ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit);
        
        depthImage = new VkImage(context, device, new VkImageSpecification(ImageType.Type2D,
            renderSupplier.CurrentRenderExtent.CastUp(), depthFormat, ImageUsageFlags.DepthStencilAttachmentBit,
            ImageViewType.Type2D, deviceProps.Limits.MaxSamplerAnisotropy, ImageAspectFlags.DepthBit));

        context.Api.GetImageMemoryRequirements(device.WrappedDevice, bufferImage.WrappedImage, out var requirements);
        context.Api.GetImageMemoryRequirements(device.WrappedDevice, depthImage.WrappedImage, out var depthRequirements);
        
        imageMemory = context.RenderServices.MemoryLessor.LeaseMemory(MemoryGroups.RgbaSampledImageMemory, requirements.Size, (uint)requirements.Alignment);
        depthImageMemory = context.RenderServices.MemoryLessor.LeaseMemory(MemoryGroups.DepthImageMemory, depthRequirements.Size, (uint)depthRequirements.Alignment);

        bufferImage.BindToMemory(imageMemory);
        bufferImage.CreateView();
        
        depthImage.BindToMemory(depthImageMemory);
        depthImage.CreateView();

        inFlightFences = new IVkFence[maxConcurrentFrames];
        for (var i = 0; i < maxConcurrentFrames; i++)
        {
            inFlightFences[i] = new VkFence(context, device, true);
        }

        logger = context.FactoryProvider.LoggerFactory.Create(GetType().Name);
        frameBuffer = new VkFrameBuffer(context, device, renderPass, [bufferImage.Views[0], depthImage.Views[0]], renderSupplier);
    }
    
    public IVkTask CreateFramePreparationTask(uint frameIndex)
    {
        return new VkHostTask(PipelineStageFlags.None, () =>
        {
            lastPrepTime = DateTime.Now;
            return VkTaskResult.Success;
        });
    }
    
    public IVkTask CreatePresentationTask(Queue queue)
    {
        return new VkHostTask(PipelineStageFlags.LateFragmentTestsBit, () =>
        {
            logger.Information("Rendered frame in: {TotalMilliseconds}ms", (DateTime.Now - lastPrepTime).TotalMilliseconds);
            return VkTaskResult.Success;
        });
    }
    
    public Framebuffer GetAvailableFramebuffer()
    {
        return frameBuffer.WrappedBuffer;
    }

    public ImageView GetAvailableImageView()
    {
        return bufferImage.Views[0];
    }

    public IVkImage GetCurrentImage()
    {
        return bufferImage;
    }

    public IVkImage TryGetCurrentDepthImage()
    {
        return depthImage;
    }

    public void Dispose()
    {
        bufferImage.Dispose();
        depthImage.Dispose();
        
        imageMemory.Dispose();
        depthImageMemory.Dispose();
        
        frameBuffer.Dispose();
        commandPool.Dispose();
        inFlightFences.DisposeAll();
        eventSubject.Dispose();
    }
}