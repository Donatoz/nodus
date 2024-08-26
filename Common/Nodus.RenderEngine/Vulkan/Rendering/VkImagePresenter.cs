using System.Reactive.Subjects;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Memory;
using Nodus.RenderEngine.Vulkan.Meta;
using Nodus.RenderEngine.Vulkan.Sync;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace Nodus.RenderEngine.Vulkan.Rendering;

public class VkImagePresenter : IVkRenderPresenter
{
    public IObservable<RenderPresentEvent> EventStream => eventSubject;

    private readonly IVkMemory imageMemory;
    private readonly IVkImage bufferImage;
    private readonly IVkFrameBuffer frameBuffer;
    private readonly IVkFence[] inFlightFences;
    private readonly IVkContext context;
    private readonly IVkLogicalDevice device;
    private readonly IVkPhysicalDevice physicalDevice;
    private readonly IVkRenderSupplier supplier;
    private readonly VkQueueInfo queueInfo;
    private readonly IVkCommandPool commandPool;
    
    private readonly Subject<RenderPresentEvent> eventSubject;
    private int frameCounter;

    public VkImagePresenter(IVkContext context, IVkLogicalDevice device, IVkPhysicalDevice physicalDevice, IVkRenderSupplier renderSupplier,
        IVkRenderPass renderPass, VkQueueInfo queueInfo, uint maxConcurrentFrames)
    {
        this.context = context;
        this.device = device;
        this.physicalDevice = physicalDevice;
        supplier = renderSupplier;
        this.queueInfo = queueInfo;

        eventSubject = new Subject<RenderPresentEvent>();
        commandPool = new VkCommandPool(context, device, queueInfo, 1, CommandPoolCreateFlags.ResetCommandBufferBit);
        imageMemory = new VkMemory(context, device, physicalDevice, MemoryPropertyFlags.DeviceLocalBit);
        
        var deviceProps = context.Api.GetPhysicalDeviceProperties(physicalDevice.WrappedDevice);
        bufferImage = new VkImage(context, device, new VkImageContext(imageMemory, ImageType.Type2D, 
            renderSupplier.CurrentRenderExtent.CastUp(), Format.B8G8R8A8Srgb, ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.TransferSrcBit,
            ImageViewType.Type2D, deviceProps.Limits.MaxSamplerAnisotropy));
        
        imageMemory.AllocateForImage(context, bufferImage.WrappedImage, device);
        bufferImage.BindToMemory();
        bufferImage.CreateView();

        inFlightFences = new IVkFence[maxConcurrentFrames];
        for (var i = 0; i < maxConcurrentFrames; i++)
        {
            inFlightFences[i] = new VkFence(context, device, true);
        }

        frameBuffer = new VkFrameBuffer(context, device, renderPass, bufferImage.View!.Value, renderSupplier);
    }
    
    public bool TryPrepareNewFrame(IVkSemaphore semaphore, uint frameIndex)
    {
        return true;
    }

    public void ProcessRenderQueue(Queue queue, IVkSemaphore semaphore, IVkFence fence)
    {
        fence.Await();

        if (++frameCounter == 5)
        {
            ExportImageMemory();
        }
    }

    private unsafe void ExportImageMemory()
    {
        var imgSize = supplier.CurrentRenderExtent.Width * supplier.CurrentRenderExtent.Height * 4;
            
        var cmdBuffer = commandPool.GetBuffer(0);
        var queue = device.TryGetGraphicsQueue(queueInfo).NotNull();
        var regions = new BufferImageCopy
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,
            ImageExtent = supplier.CurrentRenderExtent.CastUp(),
            ImageOffset = new Offset3D(0, 0, 0),
            ImageSubresource = new ImageSubresourceLayers
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseArrayLayer = 0,
                LayerCount = 1,
                MipLevel = 0
            }
        };
        using var stagingImageBuffer = new VkAllocatedBuffer<byte>(context, device, physicalDevice,
            new VkBufferContext(supplier.CurrentRenderExtent.Width * supplier.CurrentRenderExtent.Height * 4, 
                BufferUsageFlags.TransferDstBit, SharingMode.Exclusive,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit));
        stagingImageBuffer.Allocate();
        
        cmdBuffer.BeginBuffer(context);
        
        bufferImage.CmdTransitionLayout(commandPool.GetBuffer(0), ImageLayout.TransferSrcOptimal);
        context.Api.CmdCopyImageToBuffer(cmdBuffer, bufferImage.WrappedImage, ImageLayout.TransferSrcOptimal, stagingImageBuffer.WrappedBuffer, 1, &regions);
        bufferImage.CmdTransitionLayout(commandPool.GetBuffer(0), ImageLayout.General);

        cmdBuffer.SubmitBuffer(context, queue);
        
        stagingImageBuffer.MapToHost();
        var data = stagingImageBuffer.GetMappedData(imgSize);

        using var img = Image.LoadPixelData<Bgra32>(data, (int)supplier.CurrentRenderExtent.Width,
            (int)supplier.CurrentRenderExtent.Height);
        img.SaveAsPng(@"J:\RenderResults\test.png");
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