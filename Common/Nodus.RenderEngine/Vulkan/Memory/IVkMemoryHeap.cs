using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Memory;

/// <summary>
/// Represents meta-information about virtual memory heap.
/// </summary>
public interface IVkMemoryHeapInfo
{
    string HeapId { get; }
    ulong Size { get; set; }
    MemoryPropertyFlags MemoryProperties { get; }
    IVkHeapMemoryAllocator Allocator { get; }
    
    IVkFragmentationAnalyzer? FragmentationAnalyzer { get; }
    IVkDefragmenter? Defragmenter { get; }
    IVkHeapAnalyzer[]? Analyzers { get; }
}

/// <summary>
/// Represents a virtual memory heap that manages Vulkan memory allocations using the bound internal memory.
/// </summary>
public interface IVkMemoryHeap : IVkUnmanagedHook
{
    IVkMemoryHeapInfo Meta { get; }

    IVkMemoryLease LeaseMemory(ulong size, uint alignment = 1);
    VkMemoryRegion[] GetOccupiedRegions();
    ulong GetOccupiedMemory();
    double GetCurrentFragmentation();
}

public class VkMemoryHeapInfo(
    string heapId, 
    ulong size, 
    MemoryPropertyFlags memoryProperties, 
    IVkHeapMemoryAllocator allocator,
    IVkFragmentationAnalyzer? fragmentationAnalyzer = null,
    IVkDefragmenter? defragmenter = null,
    IVkHeapAnalyzer[]? analyzers = null) 
    : IVkMemoryHeapInfo
{
    public string HeapId { get; } = heapId;
    public ulong Size { get; set; } = size;
    public MemoryPropertyFlags MemoryProperties { get; } = memoryProperties;
    public IVkHeapMemoryAllocator Allocator { get; } = allocator;
    
    public IVkFragmentationAnalyzer? FragmentationAnalyzer { get; } = fragmentationAnalyzer;
    public IVkDefragmenter? Defragmenter { get; } = defragmenter;
    public IVkHeapAnalyzer[]? Analyzers { get; } = analyzers;
}