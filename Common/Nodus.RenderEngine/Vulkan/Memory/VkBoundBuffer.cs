using Nodus.RenderEngine.Vulkan.Extensions;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Nodus.RenderEngine.Vulkan.Memory;

public interface IVkBoundBufferContext
{
    ulong Size { get; }
    BufferUsageFlags Usage { get; }
    SharingMode SharingMode { get; }
    IVkMemory Memory { get; }
    ulong MemoryOffset { get; }
}

public readonly struct VkBoundBufferContext(ulong size, BufferUsageFlags usage, SharingMode sharingMode, IVkMemory memory, ulong memoryOffset = 0)
    : IVkBoundBufferContext
{
    public ulong Size { get; } = size;
    public BufferUsageFlags Usage { get; } = usage;
    public SharingMode SharingMode { get; } = sharingMode;
    public IVkMemory Memory { get; } = memory;
    public ulong MemoryOffset { get; } = memoryOffset;
}

public interface IVkBoundBuffer : IVkBuffer
{
    void BindToMemory();
    void UpdateData<T>(Span<T> data, ulong offset) where T : unmanaged;
}

public class VkBoundBuffer : VkObject, IVkBoundBuffer
{
    public Buffer WrappedBuffer { get; }
    public ulong Size => bufferContext.Size;

    private readonly IVkLogicalDevice device;
    private readonly IVkBoundBufferContext bufferContext;

    public unsafe VkBoundBuffer(IVkContext vkContext, IVkLogicalDevice device, IVkBoundBufferContext bufferContext) : base(vkContext)
    {
        this.device = device;
        this.bufferContext = bufferContext;

        var createInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = bufferContext.Size,
            Usage = bufferContext.Usage,
            SharingMode = bufferContext.SharingMode
        };

        Buffer buffer;
        Context.Api.CreateBuffer(device.WrappedDevice, in createInfo, null, &buffer)
            .TryThrow($"Failed to create buffer: {createInfo.Usage}, Size={createInfo.Size}");

        WrappedBuffer = buffer;
    }

    public void BindToMemory()
    {
        if (bufferContext.Memory.WrappedMemory == null)
        {
            throw new Exception("Failed to bind buffer: memory was not allocated.");
        }
        
        Context.Api.BindBufferMemory(device.WrappedDevice, WrappedBuffer, bufferContext.Memory.WrappedMemory.Value, bufferContext.MemoryOffset);
    }

    public unsafe void UpdateData<T>(Span<T> data, ulong offset) where T : unmanaged
    {
        if (bufferContext.Memory.WrappedMemory == null)
        {
            throw new Exception("Failed to update buffer data: memory was not allocated.");
        }
        
        void* bufferData;
        
        Context.Api.MapMemory(device.WrappedDevice, bufferContext.Memory.WrappedMemory.Value, offset, (ulong)(sizeof(T) * data.Length), 0, &bufferData)
            .TryThrow("Failed to map buffer memory.");
        
        data.CopyTo(new Span<T>(bufferData, data.Length));
        
        Context.Api.UnmapMemory(device.WrappedDevice, bufferContext.Memory.WrappedMemory.Value);
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            Context.Api.DestroyBuffer(device.WrappedDevice, WrappedBuffer, null);
        }
        
        base.Dispose(disposing);
    }
}