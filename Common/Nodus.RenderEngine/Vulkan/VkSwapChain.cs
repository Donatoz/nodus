using Nodus.Core.Extensions;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Meta;
using Nodus.RenderEngine.Vulkan.Sync;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Nodus.RenderEngine.Vulkan;

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
    PresentModeKHR PresentMode { get; }
    SwapchainKHR WrappedSwapChain { get; }
    KhrSwapchain SwapChainExtension { get; }
    ImageView[] Views { get; }
    Framebuffer[]? FrameBuffers { get; }

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
    /// <param name="newContext">The new context for the swapchain. If no provided - the state will be re-created
    /// using to the current one.</param>
    void RecreateState(IVkSwapChainContext? newContext = null);
}

public class VkSwapChain : VkObject, IVkSwapChain
{
    public Extent2D Extent { get; private set; }
    public SurfaceFormatKHR SurfaceFormat { get; private set; }
    public PresentModeKHR PresentMode { get; private set; }
    public SwapchainKHR WrappedSwapChain { get; private set; }
    public KhrSwapchain SwapChainExtension { get; }
    public ImageView[] Views { get; private set; } = null!;
    public Framebuffer[]? FrameBuffers { get; protected set; }

    protected IVkSwapChainContext SwapChainContext { get; private set; }
    protected Image[] Images { get; private set; } = null!;

    private readonly IVkLogicalDevice device;
    
    public VkSwapChain(IVkContext vkContext, IVkSwapChainContext swapChainContext, IVkExtensionProvider extensionProvider) : base(vkContext)
    {
        if (!extensionProvider.TryGetCurrentDeviceExtension(out KhrSwapchain swapChainExt))
        {
            throw new Exception($"Failed to create swapchain: {KhrSwapchain.ExtensionName} is not supported.");
        }
        
        SwapChainExtension = swapChainExt;
        device = swapChainContext.LogicalDevice;
        SwapChainContext = swapChainContext;
        

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
        
        if (SwapChainExtension.CreateSwapchain(SwapChainContext.LogicalDevice.WrappedDevice, in createInfo, null, out var swapChain) != Result.Success)
        {
            throw new Exception("Failed to create swapchain.");
        }
        
        WrappedSwapChain = swapChain;
        
        CreateImages(imageCount);
    }

    private unsafe void CreateImages(uint imageCount)
    {
        SwapChainExtension.GetSwapchainImages(SwapChainContext.LogicalDevice.WrappedDevice, WrappedSwapChain, ref imageCount, null)
            .TryThrow("Failed to get swapchain image count.");
        
        var images = new Image[imageCount];

        if (images.Any())
        {
            fixed (Image* p = images)
            {
                SwapChainExtension.GetSwapchainImages(SwapChainContext.LogicalDevice.WrappedDevice, WrappedSwapChain, ref imageCount, p)
                    .TryThrow("Failed to get swapchain images.");
            }
        }

        Images = images;

        var views = new ImageView[Images.Length];

        for (var i = 0; i < Images.Length; i++)
        {
            var viewCreateInfo = GetImageViewInfo(i);
            var view = new ImageView();
            
            Context.Api.CreateImageView(SwapChainContext.LogicalDevice.WrappedDevice, in viewCreateInfo, null, &view)
                .TryThrow($"Failed to create image view for image: {i}");
            
            views[i] = view;
        }

        Views = views;
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
        
        Console.WriteLine($"Extent: {extent.Width}, {extent.Height}");

        return extent;
    }

    protected virtual ImageViewCreateInfo GetImageViewInfo(int imageIndex)
    {
        return new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = Images[imageIndex],
            ViewType = ImageViewType.Type2D,
            Format = SurfaceFormat.Format,
            Components = new ComponentMapping
            {
                R = ComponentSwizzle.Identity,
                G = ComponentSwizzle.Identity,
                B = ComponentSwizzle.Identity,
                A = ComponentSwizzle.Identity,
            },
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };
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
        FrameBuffers = new Framebuffer[Views.Length];
        
        for (var i = 0; i < Views.Length; i++)
        {
            var view = Views[i];
            var framebufferInfo = CreateFrameBufferInfo(pass, 1, &view);
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
        if (Views.Any())
        {
            Views.ForEach(x => Context.Api.DestroyImageView(SwapChainContext.LogicalDevice.WrappedDevice, x, null));
        }

        Views = [];
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
        }
        
        base.Dispose(disposing);
    }
}