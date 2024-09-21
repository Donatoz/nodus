using System.Reactive.Linq;

namespace Nodus.RenderEngine.Vulkan.Memory;

/// <summary>
/// Represents a <see cref="IVkMemory"/>-to-<see cref="IVkMemoryLease"/> adapter.
/// </summary>
public sealed class VkLeaseAdapter : IVkMemoryLease
{
    public IVkMemory Memory { get; }
    public VkMemoryRegion Region { get; }
    public ulong Alignment { get; }
    public bool IsMapped { get; private set; }
    public IObservable<IVkMemoryLease> MutationStream => Observable.Empty<IVkMemoryLease>();
    
    private readonly IVkContext context;
    private readonly IVkLogicalDevice device;
    private unsafe void* mappedData;

    public VkLeaseAdapter(IVkContext context, IVkMemory boundMemory, ulong size, ulong alignment)
    {
        this.context = context;
        device = this.context.RenderServices.Devices.LogicalDevice;
        
        Memory = boundMemory;
        Region = new VkMemoryRegion(0, size);
        Alignment = alignment;
    }
    
    public unsafe void MapToHost()
    {
        ValidateAllocationState();
        
        void* data;
        context.Api.MapMemory(device.WrappedDevice, Memory.WrappedMemory!.Value, Region.Offset, Region.Size, 0, &data);

        mappedData = data;
        IsMapped = true;
    }

    public unsafe void Unmap()
    {
        ValidateAllocationState();
        
        if (mappedData == null)
        {
            throw new Exception("Failed to unmap memory: memory was not mapped.");
        }
        
        context.Api.UnmapMemory(device.WrappedDevice, Memory.WrappedMemory!.Value);

        mappedData = null;
        IsMapped = false;
    }

    private void ValidateAllocationState()
    {
        if (Memory.WrappedMemory == null)
        {
            throw new Exception("Failed to map memory: memory was not allocated.");
        }
    }

    // TODO: This is a duplicate of the Lease functionality. Move this to a separate layer.
    public unsafe void SetMappedData(nint dataPtr, ulong size, ulong offset)
    {
        Buffer.MemoryCopy((void*)dataPtr, (ulong*)((ulong)mappedData + offset + Region.Offset), size, size);
    }

    public unsafe Span<T> GetMappedData<T>(ulong size, ulong offset) where T : unmanaged
    {
        return new Span<T>((ulong*)((ulong)mappedData + offset + Region.Offset), (int)(size / (ulong)sizeof(T)));
    }
    
    public unsafe void Dispose()
    {
        if (mappedData != null)
        {
            Unmap();
        }
    }
}