using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Memory;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan;

public interface IVkImageSpecification
{
    ImageType Type { get; }
    Extent3D Size { get; }
    uint MipLevels { get; }
    uint ArrayLayers { get; }
    Format Format { get; }
    ImageUsageFlags Usage { get; }
    SharingMode SharingMode { get; }
    ImageViewType ViewType { get; }
    ImageAspectFlags Aspect { get; }
    float MaxAnisotropy { get; }
    
    IVkImageTransitionResolver? TransitionResolver { get; }
}

public interface IVkImageContext : IVkImageSpecification
{
    IVkMemory Memory { get; }
}

public record VkImageContext(
    IVkMemory Memory,
    ImageType Type,
    Extent3D Size,
    Format Format,
    ImageUsageFlags Usage,
    ImageViewType ViewType,
    float MaxAnisotropy,
    ImageAspectFlags Aspect = ImageAspectFlags.ColorBit,
    SharingMode SharingMode = SharingMode.Exclusive,
    uint MipLevels = 1,
    uint ArrayLayers = 1,
    IVkImageTransitionResolver? TransitionResolver = null)
    : IVkImageContext;

public interface IVkImage : IVkUnmanagedHook
{
    Image WrappedImage { get; }
    ImageView? View { get; }
    IVkImageSpecification Specification { get; }
    Sampler Sampler { get; }
    ImageLayout CurrentLayout { get; }
    
    void CmdTransitionLayout(CommandBuffer buffer, ImageLayout newLayout);
    void BindToMemory();
    void CreateView();
}

public class VkImage : VkObject, IVkImage
{
    public Image WrappedImage { get; }
    public ImageView? View { get; private set; }
    public Sampler Sampler { get; private set; }
    public ImageLayout CurrentLayout { get; private set; }

    public IVkImageSpecification Specification => imageContext;

    private readonly IVkLogicalDevice device;
    private readonly IVkImageContext imageContext;
    private readonly IVkImageTransitionResolver transitionResolver;

    public unsafe VkImage(IVkContext vkContext, IVkLogicalDevice device, IVkImageContext imageContext) : base(vkContext)
    {
        this.device = device;
        this.imageContext = imageContext;
        transitionResolver = imageContext.TransitionResolver ?? new VkImageTransitionResolver();
        CurrentLayout = ImageLayout.Undefined;

        var createInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = imageContext.Type,
            Extent = imageContext.Size,
            MipLevels = imageContext.MipLevels,
            ArrayLayers = imageContext.ArrayLayers,
            Format = imageContext.Format,
            Tiling = ImageTiling.Optimal,
            InitialLayout = ImageLayout.Undefined,
            Usage = imageContext.Usage,
            SharingMode = imageContext.SharingMode,
            Samples = SampleCountFlags.Count1Bit
        };

        Context.Api.CreateImage(device.WrappedDevice, &createInfo, null, out var image);

        WrappedImage = image;
        
        CreateSampler();
    }

    public unsafe void CreateView()
    {
        var createInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = WrappedImage,
            ViewType = imageContext.ViewType,
            Format = imageContext.Format,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = imageContext.Aspect,
                BaseMipLevel = 0,
                LevelCount = imageContext.MipLevels,
                BaseArrayLayer = 0,
                LayerCount = imageContext.ArrayLayers
            }
        };

        Context.Api.CreateImageView(device.WrappedDevice, &createInfo, null, out var view);

        View = view;
    }

    // TODO: To separate vk type
    private unsafe void CreateSampler()
    {
        var samplerInfo = new SamplerCreateInfo
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,
            AnisotropyEnable = Vk.True,
            MaxAnisotropy = imageContext.MaxAnisotropy,
            BorderColor = BorderColor.FloatOpaqueBlack,
            UnnormalizedCoordinates = Vk.False,
            CompareEnable = Vk.False,
            CompareOp = CompareOp.Always,
            MipmapMode = SamplerMipmapMode.Linear,
            MipLodBias = 0f,
            MinLod = 0f,
            MaxLod = 0f
        };

        Context.Api.CreateSampler(device.WrappedDevice, &samplerInfo, null, out var sampler);

        Sampler = sampler;
    }

    public unsafe void CmdTransitionLayout(CommandBuffer buffer, ImageLayout newLayout)
    {
        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = CurrentLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = WrappedImage,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = imageContext.Aspect,
                BaseMipLevel = 0,
                LevelCount = imageContext.MipLevels,
                BaseArrayLayer = 0,
                LayerCount = imageContext.ArrayLayers
            }
        };

        var srcStage = PipelineStageFlags.None;
        var dstStage = PipelineStageFlags.None;

        transitionResolver.ResolveOldLayout(CurrentLayout, ref srcStage, ref barrier);
        transitionResolver.ResolveNewLayout(newLayout, ref dstStage, ref barrier);
        
        Context.Api.CmdPipelineBarrier(buffer, srcStage, dstStage, 0, 0, 
            null, 0, null, 1, &barrier);
    }

    public void BindToMemory()
    {
        if (!imageContext.Memory.IsAllocated())
        {
            throw new Exception("Failed to bind image: memory was not allocated.");
        }

        Context.Api.BindImageMemory(device.WrappedDevice, WrappedImage, imageContext.Memory.WrappedMemory!.Value, 0);
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            Context.Api.DestroySampler(device.WrappedDevice, Sampler, null);
            if (View != null)
            {
                Context.Api.DestroyImageView(device.WrappedDevice, View.Value, null);
            }
            Context.Api.DestroyImage(device.WrappedDevice, WrappedImage, null);
        }
        
        base.Dispose(disposing);
    }
}