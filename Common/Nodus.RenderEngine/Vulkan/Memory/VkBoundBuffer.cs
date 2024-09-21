using Nodus.Core.Extensions;
using Nodus.RenderEngine.Vulkan.Extensions;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Nodus.RenderEngine.Vulkan.Memory;

public interface IVkBoundBuffer : IVkBuffer
{
    void BindToMemory(IVkMemoryLease lease);
    unsafe void SetMappedMemory(void* data, ulong size, ulong offset);
    Span<T> GetMappedMemory<T>(ulong size, ulong offset) where T : unmanaged;
}

public class VkBoundBuffer : VkObject, IVkBoundBuffer
{
    public Buffer WrappedBuffer { get; private set; }
    public ulong Size => bufferContext.Size;

    private readonly IVkLogicalDevice device;
    private readonly IVkBufferContext bufferContext;

    private IVkMemoryLease? memory;
    private IDisposable? leaseMutationContract;
    private ulong currentMemoryOffset;
    
    public VkBoundBuffer(IVkContext vkContext, IVkLogicalDevice device, IVkBufferContext bufferContext) : base(vkContext)
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

        var memoryWasMapped = memory!.IsMapped;

        if (!memoryWasMapped)
        {
            memory.MapToHost();
        }

        fixed (void* p = data)
        {
            memory.SetMappedData((nint)p, (ulong)(data.Length * sizeof(T)), offset);
        }

        if (!memoryWasMapped)
        {
            memory.Unmap();
        }
    }

    public unsafe void SetMappedMemory(void* data, ulong size, ulong offset)
    {
        ValidateAllocationState();
        
        memory!.SetMappedData((nint)data, size, offset);
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
            bufferContext.BlockingFences?.ForEach(x => x.Await());

            // The buffer has to be recreated between frames, and, most importantly, outside any command buffer.
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