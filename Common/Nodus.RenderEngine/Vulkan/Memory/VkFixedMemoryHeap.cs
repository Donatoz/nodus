using System.Collections.Concurrent;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Meta;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Memory;

/// <summary>
/// Represents a virtual memory heap that has a fixed memory allocation of a predefined type and size.
/// The heap tracks all memory leases and frees all of them once disposed. Each memory lease represents a homogenous
/// stream of bytes. 
/// </summary>
public class VkFixedMemoryHeap : VkObject, IVkMemoryHeap
{
    // The strategy for leasing memory in this heap is straight-forward: we select the best-fitting range for the
    // lease size, reducing the free blocks of memory.
    public IVkMemoryHeapInfo Meta { get; }

    private readonly IVkContext vkContext;
    private readonly IVkLogicalDevice device;
    private readonly IVkBoundBuffer heapBuffer;
    private readonly object syncRoot;
    
    private readonly ConcurrentDictionary<ulong, Lease> leases;
    private readonly SortedSet<VkMemoryRegion> freeRegions;

    private IVkMemory memory;

    public VkFixedMemoryHeap(IVkContext vkContext, IVkLogicalDevice device, IVkPhysicalDevice physicalDevice, IVkMemoryHeapInfo meta) : base(vkContext)
    {
        leases = new ConcurrentDictionary<ulong, Lease>();
        freeRegions = new SortedSet<VkMemoryRegion>(Comparer<VkMemoryRegion>.Create((a, b) => a.Size.CompareTo(b.Size)));

        this.vkContext = vkContext;
        this.device = device;
        Meta = meta;
        syncRoot = new object();
        
        memory = new VkMemory(Context, device, physicalDevice, Meta.MemoryProperties);
        heapBuffer = new VkBoundBuffer(vkContext, device,
            new VkBoundBufferContext(meta.Size, BufferUsageFlags.StorageBufferBit, SharingMode.Exclusive, memory));
        
        AllocateMemory();
        
        heapBuffer.AddDependency(memory);
        heapBuffer.BindToMemory();

        freeRegions.Add(new VkMemoryRegion(0, Meta.Size));
    }

    private void AllocateMemory()
    {
        memory.AllocateForBuffer(vkContext, heapBuffer.WrappedBuffer, device);
        AddDependency(memory);
    }

    public IVkMemoryLease LeaseMemory(ulong size, uint alignment = 1)
    {
        VkMemoryRegion annexedRegion;
        
        // TODO: implement lock-free sync (at least using incremental interlocked offset locator).
        lock (syncRoot)
        {
            var bestRegion = GetAvailableRegion(size, alignment);
            
            freeRegions.Remove(bestRegion.region);

            // Get the offset of the free region split point. If the alignment was not specified, it is always the offset
            // of the picked region.
            var splitPoint = bestRegion.region.Offset + bestRegion.alignedOffset;
            annexedRegion = new VkMemoryRegion(splitPoint, size);
            
            // Check the antecedent space. If it is not zero: mark it as a free region.
            if (splitPoint != bestRegion.region.Offset)
            {
                freeRegions.Add(new VkMemoryRegion(bestRegion.region.Offset, bestRegion.alignedOffset));
            }
        
            // Check the subsequent space. The same logic as with the antecedent space.
            if (annexedRegion.End != bestRegion.region.End)
            {
                freeRegions.Add(new VkMemoryRegion(annexedRegion.End + 1, bestRegion.region.Offset + bestRegion.region.Size - annexedRegion.End - 1));
            }
        }
        
        var lease = new Lease(memory, annexedRegion, FreeAllocation);

        leases[annexedRegion.Offset] = lease;

        return lease;
    }

    private (VkMemoryRegion region, ulong alignedOffset) GetAvailableRegion(ulong size, uint alignment)
    {
        if (freeRegions.Count == 0)
        {
            throw new Exception($"Failed to lease memory of size ({size} bytes): memory is full.");
        }
        
        foreach (var region in freeRegions)
        {
            var alignedOffset = (region.Offset + alignment - 1) & ~(alignment - 1);

            if (region.Size + region.Offset - alignedOffset >= size)
            {
                return (region, alignedOffset - region.Offset);
            }
        }
        
        throw new Exception($"Failed to lease memory: a memory region of a valid size ({size} bytes) was not found.");
    }

    private void MergeSubsequentRegions()
    {
        var regions = freeRegions.OrderBy(x => x.Offset).ToArray();
        
        for (var i = regions.Length - 1; i >= 1; i--)
        {
            if (regions[i].Offset - regions[i - 1].End != 1) continue;
            
            freeRegions.Remove(regions[i]);
            freeRegions.Remove(regions[i - 1]);
            freeRegions.Add(new VkMemoryRegion(regions[i - 1].Offset, regions[i - 1].Size + regions[i].Size));
        }
    }

    private ulong[] GetKeyPoints()
    {
        return leases.Keys
            .Concat(leases.Values.Select(x => x.Region.Offset + x.Region.Size))
            .Concat([0ul, Meta.Size])
            .Distinct()
            .Order()
            .ToArray();
    }

    private void FreeAllocation(IVkMemoryLease lease)
    {
        lock (syncRoot)
        {
            leases.Remove(lease.Region.Offset, out _);
            freeRegions.Add(lease.Region);
            MergeSubsequentRegions();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            heapBuffer.Dispose();
            leases.Values.DisposeAll();
            memory.Dispose();
        }
        
        base.Dispose(disposing);
    }

    private ulong GetOccupiedMemory()
    {
        return leases.Aggregate(0ul, (current, lease) => current + lease.Value.Region.Size);
    }
    
#if DEBUG
    
    public void DebugMemory()
    {
        var occupiedMemory = GetOccupiedMemory();
        
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"=== Memory Lease Heap Debug: {Meta.HeapId} ===");
        Console.WriteLine($"Total allocated memory: [{Meta.Size}] bytes.");
        Console.WriteLine($"Total occupied memory: [{occupiedMemory}] bytes | {(float)occupiedMemory / Meta.Size * 100:0.00}%.");
        Console.WriteLine($"Key points: [{string.Join(", ", GetKeyPoints())}]");
        Console.WriteLine("Leases:");
        foreach (var lease in leases.OrderBy(x => x.Key))
        {
            Console.WriteLine($"Lease: Start=[{lease.Key}], End=[{lease.Key + lease.Value.Region.Size - 1}]. Total size: [{lease.Value.Region.Size}] bytes.");
        }
        
        Console.WriteLine("==========");
        Console.WriteLine("Free regions:");
        foreach (var region in freeRegions.OrderBy(x => x.Offset))
        {
            Console.WriteLine($"Region: Start=[{region.Offset}], End=[{region.End}]. Total size: [{region.Size}] bytes.");
        }
        Console.ResetColor();
    }
    
#endif

    private class Lease : IVkMemoryLease
    {
        public IVkMemory Memory { get; }
        public VkMemoryRegion Region { get; }

        private readonly Action<IVkMemoryLease> deallocationContext;

        public Lease(IVkMemory memory, VkMemoryRegion region, Action<IVkMemoryLease> deallocationContext)
        {
            Memory = memory;
            Region = region;

            this.deallocationContext = deallocationContext;
        }
        
        public void Dispose()
        {
            deallocationContext.Invoke(this);
        }
    }
}