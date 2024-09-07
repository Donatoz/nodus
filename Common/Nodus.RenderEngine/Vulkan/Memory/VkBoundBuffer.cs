using Nodus.RenderEngine.Vulkan.Extensions;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Nodus.RenderEngine.Vulkan.Memory;

public interface IVkBoundBufferContext
{
    ulong Size { get; }
    BufferUsageFlags Usage { get; }
    SharingMode SharingMode { get; }
}

public readonly struct VkBoundBufferContext(ulong size, BufferUsageFlags usage, SharingMode sharingMode)
    : IVkBoundBufferContext
{
    public ulong Size { get; } = size;
    public BufferUsageFlags Usage { get; } = usage;
    public SharingMode SharingMode { get; } = sharingMode;
}

public interface IVkBoundBuffer : IVkBuffer
{
    void BindToMemory(IVkMemoryLease lease);
    void UpdateData<T>(Span<T> data, ulong offset) where T : unmanaged;
    unsafe void SetMappedMemory(void* data, ulong size, ulong offset);
    Span<T> GetMappedMemory<T>(ulong size, ulong offset) where T : unmanaged;
}

public class VkBoundBuffer : VkObject, IVkBoundBuffer
{
    public Buffer WrappedBuffer { get; }
    public ulong Size => bufferContext.Size;

    private readonly IVkLogicalDevice device;
    private readonly IVkBoundBufferContext bufferContext;

    private IVkMemoryLease? memory;
    private IDisposable? leaseMutationContract;
    
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
    }

    public void BindToMemory(IVkMemoryLease lease)
    {
        leaseMutationContract?.Dispose();

        memory = lease;
        
        Context.Api.BindBufferMemory(device.WrappedDevice, WrappedBuffer, memory.WrappedMemory, memory.Region.Offset);
        leaseMutationContract = memory.MutationStream.Subscribe(OnLeaseMutation);

        isMemoryBound = true;
    }

    public unsafe void UpdateData<T>(Span<T> data, ulong offset) where T : unmanaged
    {
        ValidateAllocationState();
        
        memory!.MapToHost();

        fixed (void* p = data)
        {
            memory.SetMappedData(p, (ulong)(data.Length * sizeof(T)), offset);
        }
        
        memory.Unmap();
    }

    public unsafe void SetMappedMemory(void* data, ulong size, ulong offset)
    {
        ValidateAllocationState();
        
        memory!.SetMappedData(data, size, offset);
    }

    public Span<T> GetMappedMemory<T>(ulong size, ulong offset) where T : unmanaged
    {
        ValidateAllocationState();

        return memory!.GetMappedData<T>(size, offset);
    }

    public void MapToHost()
    {
        ValidateAllocationState();
        
        memory!.MapToHost();
    }

    public void Unmap()
    {
        ValidateAllocationState();
        
        memory!.Unmap();
    }
    
    private void OnLeaseMutation(IVkMemoryLease lease)
    {
        if (isMemoryBound)
        {
            // TODO: Whenever lease is mutated - the buffer shall be re-created, notifying all dependant objects that consistently
            // use the wrapped buffer handle. Those are, for instance, descriptor sets.
        }
    }

    private void ValidateMemoryLeaseState()
    {
        if (memory == null)
        {
            throw new Exception("Failed to perform buffer operation: memory was not leased.");
        }
    }

    private void ValidateAllocationState()
    {
        ValidateMemoryLeaseState();
        
        if (!memory!.Memory.IsAllocated())
        {
            throw new Exception("Failed to perform buffer operation: memory was not allocated.");
        }
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            leaseMutationContract?.Dispose();
            Context.Api.DestroyBuffer(device.WrappedDevice, WrappedBuffer, null);
        }
        
        base.Dispose(disposing);
    }
}