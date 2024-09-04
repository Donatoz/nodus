using Nodus.RenderEngine.Vulkan.Extensions;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Nodus.RenderEngine.Vulkan.Memory;

public interface IVkHeapMemoryAllocator
{
    void AllocateMemory(IVkMemory memory, IVkMemoryHeapInfo heapInfo);
}

public sealed class VkHeapMemoryAllocator(uint memoryTypeBits) : IVkHeapMemoryAllocator
{
    public void AllocateMemory(IVkMemory memory, IVkMemoryHeapInfo heapInfo)
    {
        memory.Allocate(heapInfo.Size, memoryTypeBits);
    }
}

public sealed class BufferHeapMemoryAllocator : IVkHeapMemoryAllocator
{
    private readonly IVkContext context;
    private readonly IVkLogicalDevice device;

    public BufferHeapMemoryAllocator(IVkContext context, IVkLogicalDevice device)
    {
        this.context = context;
        this.device = device;
    }
    
    public unsafe void AllocateMemory(IVkMemory memory, IVkMemoryHeapInfo heapInfo)
    {
        var buffer = CreateHeapBuffer(heapInfo);
        
        memory.AllocateForBuffer(context, buffer, device);
        
        context.Api.DestroyBuffer(device.WrappedDevice, buffer, null);
    }
    
    private unsafe Buffer CreateHeapBuffer(IVkMemoryHeapInfo heapInfo)
    {
        var createInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = heapInfo.Size,
            Usage = BufferUsageFlags.StorageBufferBit,
            SharingMode = SharingMode.Exclusive
        };

        Buffer buffer;
        context.Api.CreateBuffer(device.WrappedDevice, in createInfo, null, &buffer)
            .TryThrow($"Failed to create heap buffer.");

        return buffer;
    }
}

public sealed class ImageHeapMemoryAllocator : IVkHeapMemoryAllocator
{
    private readonly IVkContext context;
    private readonly IVkLogicalDevice device;
    private readonly Format imageFormat;
    private readonly ImageUsageFlags usageFlags;

    public ImageHeapMemoryAllocator(IVkContext context, IVkLogicalDevice device, Format imageFormat, ImageUsageFlags usageFlags)
    {
        this.context = context;
        this.device = device;
        this.imageFormat = imageFormat;
        this.usageFlags = usageFlags;
    }
    
    public void AllocateMemory(IVkMemory memory, IVkMemoryHeapInfo heapInfo)
    {
        var image = CreateHeapImage();
        
        context.Api.GetImageMemoryRequirements(device.WrappedDevice, image.WrappedImage, out var requirements);
        
        memory.Allocate(heapInfo.Size, requirements.MemoryTypeBits);
        
        image.Dispose();
    }
    
    private IVkImage CreateHeapImage()
    {
        return new VkImage(context, device, new VkImageSpecification(ImageType.Type2D,
            new Extent3D(512, 512, 1), imageFormat, usageFlags,
            ImageViewType.Type2D, 1.0f));
    }
}