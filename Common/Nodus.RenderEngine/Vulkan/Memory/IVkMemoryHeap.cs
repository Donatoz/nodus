using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Memory;

/// <summary>
/// Represents a meta-information about virtual memory heap.
/// </summary>
public interface IVkMemoryHeapInfo
{
    string HeapId { get; }
    ulong Size { get; }
    MemoryPropertyFlags MemoryProperties { get; }
    BufferUsageFlags HeapBufferUsage { get; }
}

/// <summary>
/// Represents a virtual memory heap that manages Vulkan memory allocations using the bound internal memory.
/// </summary>
public interface IVkMemoryHeap : IVkUnmanagedHook
{
    IVkMemoryHeapInfo Meta { get; }

    IVkMemoryLease LeaseMemory(ulong size, uint alignment = 1);
}

public readonly struct VkMemoryHeapInfo(string heapId, ulong size, MemoryPropertyFlags memoryProperties, BufferUsageFlags heapBufferUsage)
    : IVkMemoryHeapInfo
{
    public string HeapId { get; } = heapId;
    public ulong Size { get; } = size;
    public MemoryPropertyFlags MemoryProperties { get; } = memoryProperties;
    public BufferUsageFlags HeapBufferUsage { get; } = heapBufferUsage;
}