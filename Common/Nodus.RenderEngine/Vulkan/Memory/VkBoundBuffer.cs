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
    public Buffer WrappedBuffer { get; private set; }
    public ulong Size => bufferContext.Size;

    private readonly IVkLogicalDevice device;
    private readonly IVkBoundBufferContext bufferContext;

    private IVkMemoryLease? memory;
    private IDisposable? leaseMutationContract;
    private ulong currentMemoryOffset;
    
    public VkBoundBuffer(IVkContext vkContext, IVkLogicalDevice device, IVkBoundBufferContext bufferContext) : base(vkContext)
    {
        this.device = device;
        this.bufferContext = bufferContext;
        
        WrappedBuffer = CreateBuffer();
    }

    private unsafe Buffer CreateBuffer()
    {
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

        return buffer;
    }

    public void BindToMemory(IVkMemoryLease lease)
    {
        leaseMutationContract?.Dispose();

        memory = lease;
        
        Context.Api.BindBufferMemory(device.WrappedDevice, WrappedBuffer, memory.WrappedMemory, memory.Region.Offset);
        leaseMutationContract = memory.MutationStream.Subscribe(OnLeaseMutation);

        currentMemoryOffset = lease.Region.Offset;
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
        // TODO: Whenever lease is mutated - the buffer shall be re-created, notifying all dependant objects that consistently
        // use the wrapped buffer handle. Those are, for instance, descriptor sets.

        var leaseMemoryChanged = lease.WrappedMemory.Handle != memory!.WrappedMemory.Handle;
        var leaseRegionChanged = lease.Region.Offset != currentMemoryOffset;
        
        if (leaseMemoryChanged || leaseRegionChanged)
        {
            // The buffer has to be recreated between frames, and, most importantly, outside any command buffer.
            // TODO: Extremely bad hack, must be executed in sync with a working queue of a render dispatcher.
            Context.Api.DeviceWaitIdle(device.WrappedDevice);
            
            RecreateBuffer();
        }
    }

    private unsafe void RecreateBuffer()
    {
        var newBuffer = CreateBuffer();
        
        Context.Api.BindBufferMemory(device.WrappedDevice, newBuffer, memory!.WrappedMemory, memory.Region.Offset);
        
        Context.Api.DestroyBuffer(device.WrappedDevice, WrappedBuffer, null);

        // TODO: External entities should be notified about the handle change
        WrappedBuffer = newBuffer;
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