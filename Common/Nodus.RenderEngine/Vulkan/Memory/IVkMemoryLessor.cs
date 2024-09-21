using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Memory;

/// <summary>
/// Represents a memory lease.
/// </summary>
public interface IVkMemoryLease : IDisposable
{
    /// <summary>
    /// Lease container.
    /// </summary>
    IVkMemory Memory { get; }
    DeviceMemory WrappedMemory => Memory.WrappedMemory!.Value;
    
    VkMemoryRegion Region { get; }
    ulong Alignment { get; }
    bool IsMapped { get; }
    
    /// <summary>
    /// Represents a stream of mutations for a memory lease.
    /// </summary>
    /// <remarks>
    /// The property provides a stream of new lease mutations that represent the current lase state within a memory allocation.
    /// The stream emits the updated state value every time the lease bound memory or region changes (for example, after defragmentation).
    /// </remarks>
    IObservable<IVkMemoryLease> MutationStream { get; }

    void MapToHost();
    void Unmap();
    void SetMappedData(nint dataPtr, ulong size, ulong offset);
    Span<T> GetMappedData<T>(ulong size, ulong offset) where T : unmanaged;
}

/// <summary>
/// Represents a tracked memory lease bound to a specific object, which lifetime is trackable.
/// </summary>
public interface IVkTrackedMemoryLease : IVkMemoryLease
{
    IVkUnmanagedHook HookedObject { get; }
}

/// <summary>
/// Represents a memory lessor that provides the functionality to lease memory regions of different size and types.
/// </summary>
public interface IVkMemoryLessor : IDisposable
{
    IReadOnlyCollection<IVkMemoryHeap> AllocatedHeaps { get; }
    
    IVkMemoryLease LeaseMemory(string groupId, ulong size, uint alignment = 1);
}

