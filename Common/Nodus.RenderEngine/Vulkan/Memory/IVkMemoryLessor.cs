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
    
    VkMemoryRegion Region { get; }
}

/// <summary>
/// Represents a memory lessor that provides the functionality to lease memory regions of different size and types.
/// </summary>
public interface IVkMemoryLessor : IDisposable
{
    IVkMemoryLease LeaseMemory(string groupId, ulong size, uint alignment = 1);
}

