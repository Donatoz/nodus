using Nodus.Core.Extensions;
using Nodus.RenderEngine.Vulkan.Meta;

namespace Nodus.RenderEngine.Vulkan.Memory;

public class VkMemoryHeapLessor : VkObject, IVkMemoryLessor
{
    public IReadOnlyCollection<IVkMemoryHeap> AllocatedHeaps => heaps.Values;

    private readonly Dictionary<string, IVkMemoryHeap> heaps;
    private readonly IVkLogicalDevice device;
    private readonly IVkPhysicalDevice physicalDevice;
    
    public VkMemoryHeapLessor(IVkContext vkContext, IVkLogicalDevice device, IVkPhysicalDevice physicalDevice, IVkMemoryHeapInfo[] heapsMeta) : base(vkContext)
    {
        this.device = device;
        this.physicalDevice = physicalDevice;
        heaps = new Dictionary<string, IVkMemoryHeap>();
        
        AllocateHeaps(heapsMeta.DistinctBy(x => x.HeapId));
    }

    private void AllocateHeaps(IEnumerable<IVkMemoryHeapInfo> heapsMeta)
    {
        foreach (var meta in heapsMeta)
        {
            var heap = new VkFixedMemoryHeap(Context, device, physicalDevice, meta);
            AddDependency(heap);

            heaps[meta.HeapId] = heap;
        }
    }
    
    public IVkMemoryLease LeaseMemory(string groupId, ulong size, uint alignment = 1)
    {
        // TODO: Improvement
        // Lessor shall have a strategy for cases when a heap of the specified group was not allocated.
        // Those could be lazily-allocated heaps (or deferred ones) which are allocated whenever they are being required
        // for the first time. The lessor could be provided with a separate set of heaps metadata that are considered as
        // deferred ones.
        
        if (!heaps.TryGetValue(groupId, out var heap))
        {
            throw new ArgumentException($"Failed to allocate memory in the specified heap: heap ({groupId}) is not allocated.");
        }

        return heap.LeaseMemory(size, alignment);
    }

#if DEBUG

    public void DebugHeaps()
    {
        heaps.Values.ForEach(x => (x as VkFixedMemoryHeap)?.DebugMemory());
    }
    
#endif

    protected override void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            heaps.Values.DisposeAll();
        }
        
        base.Dispose(disposing);
    }
}