using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.Vulkan.Convention;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Memory;
using Nodus.RenderEngine.Vulkan.Meta;
using Serilog;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Nodus.RenderEngine.Vulkan.Rendering;

public interface IVkTexture : IVkUnmanagedHook
{
    /// <summary>
    /// The desired layout of the wrapped textures.
    /// Note that the texture has to read from a new texture after changing this property at runtime.
    /// </summary>
    ImageLayout DesiredLayout { get; set; }
    IVkImage? TextureImage { get; }
    
    void CmdReadFrom(CommandBuffer cmdBuffer, ITexture<Rgba32>[] textures);
}

public class VkTexture : VkObject, IVkTexture
{
    public ImageLayout DesiredLayout { get; set; }
    public IVkImage? TextureImage { get; private set; }
    
    private IVkMemoryLease? textureMemory;
    private VkBoundBuffer? imageDataBuffer;
    private readonly IVkImageSpecification specification;
    private readonly ILogger logger;
    
    public VkTexture(IVkContext context, IVkImageSpecification specification, 
        ImageLayout desiredLayout = ImageLayout.ShaderReadOnlyOptimal) : base(context)
    {
        this.specification = specification;
        DesiredLayout = desiredLayout;
        logger = Context.FactoryProvider.LoggerFactory.Create("Texture");
    }

    public unsafe void CmdReadFrom(CommandBuffer cmdBuffer, ITexture<Rgba32>[] textures)
    {
        TextureImage?.Dispose();
        textureMemory?.Dispose();
        imageDataBuffer?.Dispose();
        
        var baseTexture = textures[0];

        if (baseTexture.Width > specification.Size.Width || baseTexture.Height > specification.Size.Height)
        {
            throw new VulkanException($"Texture size must be between 0 and {specification.Size.Width}.");
        }

        TextureImage = new VkImage(Context, Context.RenderServices.Devices.LogicalDevice, specification);
        TextureImage.GetMemoryRequirements(out var requirements);
        
        var textureSize = (uint)(baseTexture.Width * baseTexture.Height * 4);
        
        // Holds the provided textures data, host-visible
        using var stgMemory = Context.RenderServices.MemoryLessor.LeaseMemory(MemoryGroups.StagingStorageMemory,
            textureSize * (ulong)textures.Length);
        stgMemory.MapToHost();
        
        // Holds actual image data, NOT host-visible
        textureMemory = Context.RenderServices.MemoryLessor.LeaseMemory(MemoryGroups.RgbaSampledImageMemory, requirements.Size, (uint)requirements.Alignment);
        imageDataBuffer = new VkBoundBuffer(Context,
            new VkBufferContext(stgMemory.Region.Size, BufferUsageFlags.TransferSrcBit, SharingMode.Exclusive));
        
        imageDataBuffer.BindToMemory(stgMemory);
        
        TextureImage.BindToMemory(textureMemory);
        TextureImage.CreateSampler();
        TextureImage.CreateView();
        
        TextureImage.CmdTransitionLayout(cmdBuffer, ImageLayout.TransferDstOptimal);
        
        for (var i = 0u; i < textures.Length; i++)
        {
            var texture = textures[i];
            var textureData = new byte[textureSize];

            // Since the textures are packed into a texture array - every single texture must have a uniform size.
            if (texture.Width != baseTexture.Width || texture.Height != baseTexture.Height)
            {
                using var clone = texture.ManagedImage.Clone();
                clone.Mutate(x => x.Resize(new Size(baseTexture.Width, baseTexture.Height)));
                clone.CopyPixelDataTo(textureData);
            }
            else
            {
                texture.ManagedImage.CopyPixelDataTo(textureData);
            }

            var offset = textureSize * i;
            
            fixed (byte* pData = textureData)
            {
                stgMemory.SetMappedData((nint)pData, textureSize, offset);
            }
        }
        
        for (var i = 0u; i < textures.Length; i++)
        {
            var imageCopy = new BufferImageCopy
            {
                BufferOffset = textureSize * i,
                BufferRowLength = 0,
                BufferImageHeight = 0,
                ImageSubresource = new ImageSubresourceLayers
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    MipLevel = 0,
                    BaseArrayLayer = i,
                    LayerCount = 1
                },
                ImageOffset = new Offset3D(0, 0, 0),
                ImageExtent = TextureImage.Specification.Size
            };
        
            Context.Api.CmdCopyBufferToImage(cmdBuffer, imageDataBuffer.WrappedBuffer, TextureImage.WrappedImage, ImageLayout.TransferDstOptimal, 1, &imageCopy);
        }
        
        TextureImage.CmdTransitionLayout(cmdBuffer, DesiredLayout);
        
        logger.Information("Read from {TexturesLength} textures", textures.Length);
    }

    protected override void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            TextureImage?.Dispose();
            imageDataBuffer?.Dispose();
            textureMemory?.Dispose();
        }
        
        base.Dispose(disposing);
    }
}