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

public record VkImageSpecification(
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
    : IVkImageSpecification;

public interface IVkImage : IVkUnmanagedHook
{
    Image WrappedImage { get; }
    IReadOnlyList<ImageView> Views { get; }
    IVkImageSpecification Specification { get; }
    Sampler? Sampler { get; }
    ImageLayout CurrentLayout { get; }
    
    void CmdTransitionLayout(CommandBuffer buffer, ImageLayout newLayout, uint arrayLevel = 0);
    void BindToMemory(IVkMemory memory);
    void CreateView(uint baseArrayLevel = 0, uint? layerCount = null);
    void CreateSampler();
}

public class VkImage : VkObject, IVkImage
{
    public Image WrappedImage { get; }
    public IReadOnlyList<ImageView> Views => views;
    public Sampler? Sampler { get; private set; }
    public ImageLayout CurrentLayout { get; private set; }

    public IVkImageSpecification Specification { get; }

    private readonly IVkLogicalDevice device;
    private readonly IVkImageTransitionResolver transitionResolver;
    private readonly List<ImageView> views;

    public unsafe VkImage(IVkContext vkContext, IVkLogicalDevice device, IVkImageSpecification specification) : base(vkContext)
    {
        this.device = device;
        Specification = specification;
        transitionResolver = Specification.TransitionResolver ?? new VkImageTransitionResolver();
        CurrentLayout = ImageLayout.Undefined;
        views = [];

        var createInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = Specification.Type,
            Extent = Specification.Size,
            MipLevels = Specification.MipLevels,
            ArrayLayers = Specification.ArrayLayers,
            Format = Specification.Format,
            Tiling = ImageTiling.Optimal,
            InitialLayout = ImageLayout.Undefined,
            Usage = Specification.Usage,
            SharingMode = Specification.SharingMode,
            Samples = SampleCountFlags.Count1Bit
        };

        Context.Api.CreateImage(device.WrappedDevice, &createInfo, null, out var image);

        WrappedImage = image;
    }

    public unsafe void CreateView(uint baseArrayLevel = 0, uint? layerCount = null)
    {
        var createInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = WrappedImage,
            ViewType = Specification.ViewType,
            Format = Specification.Format,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = Specification.Aspect,
                BaseMipLevel = 0,
                LevelCount = Specification.MipLevels,
                BaseArrayLayer = baseArrayLevel,
                LayerCount = layerCount ?? Specification.ArrayLayers
            }
        };

        Context.Api.CreateImageView(device.WrappedDevice, &createInfo, null, out var view);

        views.Add(view);
    }

    // TODO: To separate vk type
    public unsafe void CreateSampler()
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
            MaxAnisotropy = Specification.MaxAnisotropy,
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

    public unsafe void CmdTransitionLayout(CommandBuffer buffer, ImageLayout newLayout, uint baseArrayLayer = 0)
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
                AspectMask = Specification.Aspect,
                BaseMipLevel = 0,
                LevelCount = Specification.MipLevels,
                BaseArrayLayer = baseArrayLayer,
                LayerCount = Specification.ArrayLayers
            }
        };

        var srcStage = PipelineStageFlags.None;
        var dstStage = PipelineStageFlags.None;

        transitionResolver.ResolveOldLayout(CurrentLayout, ref srcStage, ref barrier);
        transitionResolver.ResolveNewLayout(newLayout, ref dstStage, ref barrier);
        
        Context.Api.CmdPipelineBarrier(buffer, srcStage, dstStage, 0, 0, 
            null, 0, null, 1, &barrier);

        CurrentLayout = newLayout;
    }

    public void BindToMemory(IVkMemory memory)
    {
        if (!memory.IsAllocated())
        {
            throw new Exception("Failed to bind image: memory was not allocated.");
        }

        Context.Api.BindImageMemory(device.WrappedDevice, WrappedImage, memory.WrappedMemory!.Value, 0);
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            if (Sampler != null)
            {
                Context.Api.DestroySampler(device.WrappedDevice, Sampler.Value, null);
            }
            views.ForEach(x => Context.Api.DestroyImageView(device.WrappedDevice, x, null));
            Context.Api.DestroyImage(device.WrappedDevice, WrappedImage, null);
        }
        
        base.Dispose(disposing);
    }
}