using Nodus.Core.Extensions;
using Nodus.RenderEngine.Vulkan.Convention;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Memory;
using Nodus.RenderEngine.Vulkan.Meta;
using Nodus.RenderEngine.Vulkan.Sync;
using Nodus.RenderEngine.Vulkan.Utility;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Nodus.RenderEngine.Vulkan.Presentation;

public interface IVkSwapChainContext
{
    IVkLogicalDevice LogicalDevice { get; }
    IVkKhrSurface Surface { get; }
    
    VkSurfaceFormatRequest SurfaceFormatRequest { get; }
    PresentModeKHR PresentMode { get; }
    IVkSwapChainContextSupplier Supplier { get; }
}

public readonly struct VkSwapChainContext(
    IVkLogicalDevice logicalDevice,
    IVkKhrSurface surface,
    VkSurfaceFormatRequest surfaceFormatRequest,
    PresentModeKHR presentMode,
    IVkSwapChainContextSupplier supplier)
    : IVkSwapChainContext
{
    public IVkLogicalDevice LogicalDevice { get; } = logicalDevice;
    public IVkKhrSurface Surface { get; } = surface;
    public VkSurfaceFormatRequest SurfaceFormatRequest { get; } = surfaceFormatRequest;
    public PresentModeKHR PresentMode { get; } = presentMode;
    public IVkSwapChainContextSupplier Supplier { get; } = supplier;
}

public readonly struct VkSurfaceFormatRequest(Format requestedFormat, ColorSpaceKHR requestedColorSpace)
{
    public Format RequestedFormat { get; } = requestedFormat;
    public ColorSpaceKHR RequestedColorSpace { get; } = requestedColorSpace;
}

/// <summary>
/// Represents a wrapped Vulkan swapchain.
/// </summary>
public interface IVkSwapChain : IVkUnmanagedHook
{
    Extent2D Extent { get; }
    SurfaceFormatKHR SurfaceFormat { get; }
    Format DepthFormat { get; }
    PresentModeKHR PresentMode { get; }
    SwapchainKHR WrappedSwapChain { get; }
    KhrSwapchain SwapChainExtension { get; }
    Framebuffer[]? FrameBuffers { get; }
    IVkImage[] Images { get; }
    IVkImage? DepthStencilImage { get; }

    /// <summary>
    /// Acquire the next image in the swap chain for rendering.
    /// </summary>
    /// <param name="imageIndex">The index of the acquired image.</param>
    /// <param name="semaphore">The semaphore to signal when the image is available.</param>
    /// <param name="fence">The fence to signal when the image is available.</param>
    /// <param name="timeout">The timeout value in nanoseconds.</param>
    Result AcquireNextImage(out uint imageIndex, IVkSemaphore? semaphore = null, IVkFence? fence = null, ulong timeout = ulong.MaxValue);

    /// <summary>
    /// Create frame buffers for the swapchain according to the specified render pass. If there are existing framebuffers
    /// already created - they will be discarded.
    /// </summary>
    void CreateFrameBuffers(RenderPass pass);

    /// <summary>
    /// Discard the current state of the swapchain.
    /// </summary>
    void DiscardCurrentState();

    /// <summary>
    /// Recreate the state of the swapchain.
    /// </summary>
    /// <param name="newContext">The new context for the swapchain.
    /// If not provided - the state will be re-created using the current one.</param>
    void RecreateState(IVkSwapChainContext? newContext = null);
    
    void CmdTransitionCurrentImage(int imgIndex, CommandBuffer cmdBuffer, ImageLayout oldLayout, ImageLayout newLayout, ImageSubresourceRange subresourceRange, VkImageMemoryBarrierMasks masks);
}

public class VkSwapChain : VkObject, IVkSwapChain
{
    public Extent2D Extent { get; private set; }
    public SurfaceFormatKHR SurfaceFormat { get; private set; }
    public Format DepthFormat { get; private set; }
    public PresentModeKHR PresentMode { get; private set; }
    public SwapchainKHR WrappedSwapChain { get; private set; }
    public KhrSwapchain SwapChainExtension { get; }
    public Framebuffer[]? FrameBuffers { get; protected set; }
    public IVkImage[] Images { get; private set; } = null!;
    public IVkImage? DepthStencilImage => depthImage;

    protected IVkSwapChainContext SwapChainContext { get; private set; }

    private IVkMemoryLease depthImageMemory = null!;
    private IVkImage? depthImage;
    private ulong previousDepthMemorySize;

    private readonly IVkCommandPool commandPool;
    private readonly IVkLogicalDevice device;
    
    public VkSwapChain(IVkContext vkContext, IVkSwapChainContext swapChainContext, IVkExtensionProvider extensionProvider) : base(vkContext)
    {
        if (!extensionProvider.TryGetCurrentDeviceExtension(out KhrSwapchain swapChainExt))
        {
            throw new VulkanRenderingException($"Failed to create swapchain: {KhrSwapchain.ExtensionName} is not supported.");
        }
        
        SwapChainExtension = swapChainExt;
        device = swapChainContext.LogicalDevice;
        SwapChainContext = swapChainContext;

        commandPool = new VkCommandPool(vkContext, swapChainContext.LogicalDevice,
            vkContext.RenderServices.Devices.PhysicalDevice.QueueInfo, 1,
            CommandPoolCreateFlags.ResetCommandBufferBit);

        UpdateState();
    }

    private unsafe void UpdateState()
    {
        var queueInfo = SwapChainContext.Supplier.GetQueueInfo();
        var surfaceInfo = SwapChainContext.Supplier.GetSurfaceInfo();
        
        queueInfo.ThrowIfIncomplete();
        
        SurfaceFormat = GetFormat(surfaceInfo.Formats, SwapChainContext.SurfaceFormatRequest);
        PresentMode = GetPresentMode(surfaceInfo.PresentModes, SwapChainContext.PresentMode);
        Extent = GetSwapExtent(surfaceInfo);
        
        var imageCount = surfaceInfo.Capabilities.MinImageCount + 1;

        if (surfaceInfo.Capabilities.MaxImageCount > 0)
        {
            imageCount = Math.Min(imageCount, surfaceInfo.Capabilities.MaxImageCount);
        }

        var createInfo = new SwapchainCreateInfoKHR
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = SwapChainContext.Surface.SurfaceKhr,
            MinImageCount = imageCount,
            ImageFormat = SurfaceFormat.Format,
            ImageColorSpace = SurfaceFormat.ColorSpace,
            ImageExtent = Extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.ColorAttachmentBit,
            PreTransform = surfaceInfo.Capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = PresentMode,
            Clipped = Vk.True,
            OldSwapchain = default
        };
        
        var familyIndices = stackalloc[] { queueInfo.GraphicsFamily!.Value, queueInfo.PresentFamily!.Value };
        
        if (queueInfo.GraphicsFamily != queueInfo.PresentFamily)
        {
            createInfo.ImageSharingMode = SharingMode.Concurrent;
            createInfo.QueueFamilyIndexCount = 2;
            createInfo.PQueueFamilyIndices = familyIndices;
        }
        else
        {
            createInfo.ImageSharingMode = SharingMode.Exclusive;
        }
        
        SwapChainExtension.CreateSwapchain(SwapChainContext.LogicalDevice.WrappedDevice, in createInfo, null, out var swapChain)
            .TryThrow("Failed to create swapchain.");
        
        WrappedSwapChain = swapChain;
        
        CreateImages(imageCount);
        CreateDepthImage();
    }
    
    private void CreateDepthImage()
    {
        var physicalDevice = Context.RenderServices.Devices.PhysicalDevice;
        
        DepthFormat = ImageUtility.GetSupportedFormat(
            [Format.D32SfloatS8Uint, Format.D24UnormS8Uint],
            physicalDevice, ImageTiling.Optimal, FormatFeatureFlags.DepthStencilAttachmentBit);
        
        depthImage = new VkImage(Context, device, new VkImageSpecification(ImageType.Type2D,
            Extent.CastUp(), DepthFormat, ImageUsageFlags.DepthStencilAttachmentBit,
            ImageViewType.Type2D, physicalDevice.Properties.Limits.MaxSamplerAnisotropy, ImageAspectFlags.DepthBit | ImageAspectFlags.StencilBit));

        Context.Api.GetImageMemoryRequirements(device.WrappedDevice, depthImage.WrappedImage, out var requirements);
        
        if (previousDepthMemorySize < requirements.Size)
        {
            depthImageMemory?.Dispose();
            depthImageMemory =
                Context.RenderServices.MemoryLessor.LeaseMemory(MemoryGroups.DepthImageMemory, requirements.Size, (uint)requirements.Alignment);
            previousDepthMemorySize = depthImageMemory.Region.Size;
        }
        
        depthImage.BindToMemory(depthImageMemory);
        depthImage.CreateView();

        var cmdBuffer = commandPool.GetBuffer(0);
        
        cmdBuffer.BeginBuffer(Context);
        
        depthImage.CmdTransitionLayout(cmdBuffer, ImageLayout.DepthStencilAttachmentOptimal);
        
        VkFence.Await(Context, f => cmdBuffer.SubmitBuffer(Context, device.TryGetGraphicsQueue(Context.RenderServices.Devices.PhysicalDevice.QueueInfo)!.Value, f));
    }

    private unsafe void CreateImages(uint imageCount)
    {
        SwapChainExtension.GetSwapchainImages(SwapChainContext.LogicalDevice.WrappedDevice, WrappedSwapChain, ref imageCount, null)
            .TryThrow("Failed to get swapchain image count.");
        
        var images = new Image[imageCount];

        if (images.Length != 0)
        {
            fixed (Image* p = images)
            {
                SwapChainExtension.GetSwapchainImages(SwapChainContext.LogicalDevice.WrappedDevice, WrappedSwapChain, ref imageCount, p)
                    .TryThrow("Failed to get swapchain images.");
            }
        }

        Images = images.Select(IVkImage (x) =>
            {
                var img = new VkImage(Context, device, x, new VkImageSpecification(ImageType.Type2D, Extent.CastUp(),
                    SurfaceFormat.Format, ImageUsageFlags.None, ImageViewType.Type2D,
                    Context.RenderServices.Devices.PhysicalDevice.Properties.Limits.MaxSamplerAnisotropy));
                img.CreateView();

                return img;
            })
            .ToArray();
    }

    protected virtual SurfaceFormatKHR GetFormat(SurfaceFormatKHR[] availableFormats, VkSurfaceFormatRequest requestedFormatRequest)
    {
        SurfaceFormatKHR? sufficient = availableFormats.FirstOrDefault(x =>
            x.Format == requestedFormatRequest.RequestedFormat
            && x.ColorSpace == requestedFormatRequest.RequestedColorSpace
            );

        return sufficient ?? availableFormats.First();
    }

    protected virtual PresentModeKHR GetPresentMode(PresentModeKHR[] availableModes, PresentModeKHR requestedMode)
    {
        return availableModes.Contains(requestedMode) ? requestedMode : availableModes.First();
    }

    protected virtual Extent2D GetSwapExtent(VkSurfaceInfo surfaceInfo)
    {
        if (surfaceInfo.Capabilities.CurrentExtent.Width != uint.MaxValue)
        {
            return surfaceInfo.Capabilities.CurrentExtent;
        }

        var extent = new Extent2D
        {
            Width = Math.Clamp((uint)surfaceInfo.FrameBufferSize.X, surfaceInfo.Capabilities.MinImageExtent.Width, surfaceInfo.Capabilities.MaxImageExtent.Width),
            Height = Math.Clamp((uint)surfaceInfo.FrameBufferSize.Y, surfaceInfo.Capabilities.MinImageExtent.Height, surfaceInfo.Capabilities.MaxImageExtent.Height)
        };
        
        return extent;
    }

    public unsafe Result AcquireNextImage(out uint imageIndex, IVkSemaphore? semaphore, IVkFence? fence, ulong timeout)
    {
        var imgIndex = 0u;
        
        var res = SwapChainExtension.AcquireNextImage(device.WrappedDevice, WrappedSwapChain, timeout, 
                semaphore?.WrappedSemaphore ?? default, fence?.WrappedFence ?? default, &imgIndex);
        
        imageIndex = imgIndex;

        return res;
    }

    public virtual unsafe void CreateFrameBuffers(RenderPass pass)
    {
        TryDestroyCurrentFrameBuffers();
        FrameBuffers = new Framebuffer[Images.Length];
        
        for (var i = 0; i < Images.Length; i++)
        {
            var attachments = stackalloc[] { Images[i].Views[0], depthImage!.Views[0] };
            var framebufferInfo = CreateFrameBufferInfo(pass, 2, attachments);
            Framebuffer buffer;

            Context.Api.CreateFramebuffer(device.WrappedDevice, in framebufferInfo, null, &buffer)
                .TryThrow($"Failed to create framebuffer for image view: {i}.");

            FrameBuffers[i] = buffer;
        }
    }

    public void RecreateState(IVkSwapChainContext? context)
    {
        TryDestroyCurrentFrameBuffers();
        DiscardCurrentState();

        if (context != null)
        {
            SwapChainContext = context;
        }
        
        UpdateState();
    }

    public void CmdTransitionCurrentImage(int imgIndex, CommandBuffer cmdBuffer, ImageLayout oldLayout, ImageLayout newLayout,
        ImageSubresourceRange subresourceRange, VkImageMemoryBarrierMasks masks)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(imgIndex, Images.Length);
        
        ImageUtility.CmdTransitionLayoutManual(cmdBuffer, Context, Images[imgIndex].WrappedImage, oldLayout, newLayout, subresourceRange, masks);
    }

    protected unsafe void TryDestroyCurrentFrameBuffers()
    {
        FrameBuffers?.ForEach(x => Context.Api.DestroyFramebuffer(device.WrappedDevice, x, null));
        FrameBuffers = null;
    }

    public unsafe void DiscardCurrentState()
    {
        if (!WrappedSwapChain.Equals(default))
        {
            SwapChainExtension.DestroySwapchain(SwapChainContext.LogicalDevice.WrappedDevice, WrappedSwapChain, null);
        }
        
        Images.DisposeAll();
        
        depthImage?.Dispose();
        depthImage = null;
        
        WrappedSwapChain = default;
    }

    protected virtual unsafe FramebufferCreateInfo CreateFrameBufferInfo(RenderPass pass, uint attachmentCount, ImageView* attachments)
    {
        return new FramebufferCreateInfo
        {
            SType = StructureType.FramebufferCreateInfo,
            RenderPass = pass,
            AttachmentCount = attachmentCount,
            PAttachments = attachments,
            Width = Extent.Width,
            Height = Extent.Height,
            Layers = 1
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            TryDestroyCurrentFrameBuffers();
            DiscardCurrentState();
            depthImageMemory.Dispose();
            commandPool.Dispose();
        }
        
        base.Dispose(disposing);
    }
}