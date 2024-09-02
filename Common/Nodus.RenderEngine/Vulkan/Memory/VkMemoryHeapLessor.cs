using Nodus.Core.Extensions;
using Nodus.RenderEngine.Vulkan.Meta;

namespace Nodus.RenderEngine.Vulkan.Memory;

public class VkMemoryHeapLessor : VkObject, IVkMemoryLessor
{
    // TODO: Minor fix
    // To private
    public readonly IDictionary<string, IVkMemoryHeap> heaps;
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
        if (!heaps.TryGetValue(groupId, out var heap))
        {
            throw new ArgumentException("Failed to allocate memory in the specified heap: heap is not allocated.", nameof(groupId));
        }

        return heap.LeaseMemory(size, alignment);
    }

    protected override void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            heaps.Values.DisposeAll();
        }
        
        base.Dispose(disposing);
    }
}