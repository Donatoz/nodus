using Nodus.RenderEngine.Vulkan.Extensions;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Nodus.RenderEngine.Vulkan.Memory;

public interface IVkBoundBufferContext
{
    ulong Size { get; }
    BufferUsageFlags Usage { get; }
    SharingMode SharingMode { get; }
    IVkMemoryLease MemoryLease { get; }
}

public readonly struct VkBoundBufferContext(ulong size, BufferUsageFlags usage, SharingMode sharingMode, IVkMemoryLease memoryLease)
    : IVkBoundBufferContext
{
    public ulong Size { get; } = size;
    public BufferUsageFlags Usage { get; } = usage;
    public SharingMode SharingMode { get; } = sharingMode;
    public IVkMemoryLease MemoryLease { get; } = memoryLease;
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
    private readonly IDisposable leaseMutationContract;

    private bool isMemoryBound;

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

        leaseMutationContract = bufferContext.MemoryLease.MutationStream.Subscribe(OnLeaseMutation);
    }

    public void BindToMemory()
    {
        Console.WriteLine($"Bind to memory at offset: {bufferContext.MemoryLease.Region.Offset}");
        ValidateAllocationState();
        
        Context.Api.BindBufferMemory(device.WrappedDevice, WrappedBuffer, bufferContext.MemoryLease.WrappedMemory, bufferContext.MemoryLease.Region.Offset);

        isMemoryBound = true;
    }

    public unsafe void UpdateData<T>(Span<T> data, ulong offset) where T : unmanaged
    {
        if (!bufferContext.MemoryLease.Memory.IsAllocated())
        {
            throw new Exception("Failed to update buffer data: memory was not allocated.");
        }
        
        void* bufferData;
        
        Context.Api.MapMemory(device.WrappedDevice, bufferContext.MemoryLease.WrappedMemory, offset, (ulong)(sizeof(T) * data.Length), 0, &bufferData)
            .TryThrow("Failed to map buffer memory.");
        
        data.CopyTo(new Span<T>(bufferData, data.Length));
        
        Context.Api.UnmapMemory(device.WrappedDevice, bufferContext.MemoryLease.WrappedMemory);
    }
    
    private void OnLeaseMutation(IVkMemoryLease lease)
    {
        if (isMemoryBound)
        {
            BindToMemory();
        }
    }

    private void ValidateAllocationState()
    {
        if (!bufferContext.MemoryLease.Memory.IsAllocated())
        {
            throw new Exception("Failed to perform buffer operation: memory was not allocated.");
        }
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            leaseMutationContract.Dispose();
            Context.Api.DestroyBuffer(device.WrappedDevice, WrappedBuffer, null);
        }
        
        base.Dispose(disposing);
    }
}