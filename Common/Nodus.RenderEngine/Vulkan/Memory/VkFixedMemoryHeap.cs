using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Vulkan.Meta;

namespace Nodus.RenderEngine.Vulkan.Memory;

/// <summary>
/// Represents a virtual memory heap that has a fixed memory allocation of a predefined type and size.
/// The heap tracks all memory leases and frees them all once disposed. Each memory lease represents a homogenous
/// stream of bytes.
/// </summary>
public sealed class VkFixedMemoryHeap : VkObject, IVkMemoryHeap
{
    // The strategy for leasing memory in this heap is straight-forward: select the best-fitting range for the
    // lease size, reducing the free blocks of memory.
    
    // Default defragmentation strategy: the process takes place whenever
    //     the fragmentation value reaches a specific threshold,
    //     or if there is no valid memory region available for a lease, while there is enough free memory
    
    public IVkMemoryHeapInfo Meta { get; }

    private readonly object syncRoot;
    
    private readonly ConcurrentDictionary<ulong, Lease> leases;
    private readonly SortedSet<VkMemoryRegion> freeRegions;
    private readonly Subject<IVkMemoryLease> leaseMutationSubject;
    private readonly IVkLogicalDevice device;
    private readonly IVkPhysicalDevice physicalDevice;
    private readonly IVkFragmentationAnalyzer fragmentationAnalyzer;
    private readonly IVkDefragmenter defragmenter;
    private readonly IVkHeapAnalyzer[] analyzers;
    
    private VkMemory memory;

    public VkFixedMemoryHeap(IVkContext vkContext, IVkLogicalDevice device, IVkPhysicalDevice physicalDevice, IVkMemoryHeapInfo meta) : base(vkContext)
    {
        this.device = device;
        this.physicalDevice = physicalDevice;
        leases = new ConcurrentDictionary<ulong, Lease>();
        freeRegions = new SortedSet<VkMemoryRegion>(Comparer<VkMemoryRegion>.Create((a, b) => a.Size.CompareTo(b.Size)));
        leaseMutationSubject = new Subject<IVkMemoryLease>();
        
        fragmentationAnalyzer = meta.FragmentationAnalyzer ?? new VkEntropicFragmentationAnalyzer();
        defragmenter = meta.Defragmenter ?? new VkLinearDefragmenter();
        analyzers = meta.Analyzers ?? [new VkHeapDefragmentationAnalyzer()];

        Meta = meta;
        syncRoot = new object();
        
        memory = new VkMemory(Context, device, physicalDevice, Meta.MemoryProperties);
        AllocateMemory();
        AddDependency(memory);
        
        freeRegions.Add(new VkMemoryRegion(0, Meta.Size));
    }

    private void AllocateMemory()
    {
        Meta.Allocator.AllocateMemory(memory, Meta);
    }

    // TODO: Must not be here, as the heap is fixed.
    private void ReallocateMemory()
    {
        memory.Dispose();
        
        memory = new VkMemory(Context, device, physicalDevice, Meta.MemoryProperties);
        AllocateMemory();
        
        AddDependency(memory);

        UpdateLeasesMemory();
    }

    public IVkMemoryLease LeaseMemory(ulong size, uint alignment = 1)
    {
        VkMemoryRegion annexedRegion;
        
        // TODO: implement lock-free sync (at least using incremental interlocked offset locator).
        lock (syncRoot)
        {
            var bestRegion = GetAvailableRegion(size, alignment);
            
            freeRegions.Remove(bestRegion.region);

            // Get the offset of the free region split point.
            // If the alignment wasn't specified, it is always the offset of the picked region.
            var splitPoint = bestRegion.region.Offset + bestRegion.alignedOffset;
            annexedRegion = new VkMemoryRegion(splitPoint, size);
            
            // Check the antecedent space. If it is not zero: mark it as a free region.
            if (splitPoint != bestRegion.region.Offset)
            {
                freeRegions.Add(new VkMemoryRegion(bestRegion.region.Offset, bestRegion.alignedOffset));
            }
        
            // Check the following space. The same logic as with the antecedent space.
            if (annexedRegion.End != bestRegion.region.End)
            {
                freeRegions.Add(new VkMemoryRegion(annexedRegion.End + 1, bestRegion.region.Offset + bestRegion.region.Size - annexedRegion.End - 1));
            }
        }
        
        var lease = new Lease(memory, annexedRegion, alignment, FreeAllocation, leaseMutationSubject);

        leases[annexedRegion.Offset] = lease;

        return lease;
    }

    public VkMemoryRegion[] GetOccupiedRegions()
    {
        return leases.Values.Select(x => x.Region).ToArray();
    }

    private (VkMemoryRegion region, ulong alignedOffset) GetAvailableRegion(ulong size, uint alignment)
    {
        if (freeRegions.Count == 0)
        {
            throw new Exception($"Failed to lease memory of size ({size} bytes): memory is full.");
        }
        
        foreach (var region in freeRegions)
        {
            // Align the region offset.
            var alignedOffset = (region.Offset + alignment - 1) & ~(alignment - 1);

            // Take the alignment into account when determining whether the memory region is large enough.
            if (region.Size + region.Offset - alignedOffset >= size)
            {
                // Provide the relative (to the region start offset) aligned offset just for convenience sake.
                return (region, alignedOffset - region.Offset);
            }
        }

        // If there is enough free memory, but still no available free region - execute defragmentation and try again.
        if (Meta.Size - GetOccupiedMemory() >= size + alignment)
        {
            Defragment();
            return GetAvailableRegion(size, alignment);
        }
        
        throw new Exception($"Failed to lease memory: a memory region of a valid size ({size} bytes) was not found.");
    }

    private void MergeSubsequentRegions()
    {
        // To perform subsequent merges, the memory regions must be ordered by their offsets.
        var regions = freeRegions.OrderBy(x => x.Offset).ToArray();
        
        for (var i = regions.Length - 1; i >= 1; i--)
        {
            if (regions[i].Offset - regions[i - 1].End != 1) continue;
            
            freeRegions.Remove(regions[i]);
            freeRegions.Remove(regions[i - 1]);
            freeRegions.Add(new VkMemoryRegion(regions[i - 1].Offset, regions[i - 1].Size + regions[i].Size));
        }
    }

    private void AnalyzeMemory()
    {
        analyzers.ForEach(x => InterpretAnalysisResult(x.RequestAnalysis(this)));
    }

    private void InterpretAnalysisResult(HeapAnalysisResult result)
    {
        switch (result)
        {
            case HeapAnalysisResult.DefragmentationRequired:
                Defragment();
                break;
            case HeapAnalysisResult.NoChangeRequired:
            default:
                return;
        }
    }
    
    private void UpdateLeasesMemory()
    {
        leases.Values.ForEach(x =>
        {
            x.UpdateMemory(memory);
            leaseMutationSubject.OnNext(x);
        });
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
            AnalyzeMemory();
        }
    }

    public void Defragment()
    {
#if DEBUG
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Heap ({Meta.HeapId}) is being de-fragmented.");
        Console.ResetColor();
        var startTime = DateTime.Now;
#endif
        
        var realignedLeaseRegions = defragmenter.Defragment(leases.Values.Select(x => (x.Region, x.Alignment)));
        
        // TODO: Optimization
        // Optimize the regions lookup.
        leases.Values.ForEach(x =>
        {
            x.UpdateRegion(realignedLeaseRegions.First(r => r.Size == x.Region.Size));
            leaseMutationSubject.OnNext(x);
        });

        lock (syncRoot)
        {
            freeRegions.Clear();
            freeRegions.Add(new VkMemoryRegion(realignedLeaseRegions[^1].End + 1, Meta.Size - realignedLeaseRegions[^1].End));
        }
        
#if DEBUG
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"Defragmentation process took: {(DateTime.Now - startTime).TotalMilliseconds} ms.");
        Console.ResetColor();
#endif
    }

    public double GetCurrentFragmentation()
    {
        lock (syncRoot)
        {
            return fragmentationAnalyzer.EvaluateFragmentation(freeRegions, Meta.Size);
        }
    }
    
    public ulong GetOccupiedMemory()
    {
        return leases.Aggregate(0ul, (current, lease) => current + lease.Value.Region.Size);
    }

    protected override void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            leases.Values.DisposeAll();
            leaseMutationSubject.Dispose();
            memory.Dispose();
        }
        
        base.Dispose(disposing);
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
        lock (syncRoot)
        {
            foreach (var region in freeRegions.OrderBy(x => x.Offset))
            {
                Console.WriteLine($"Region: Start=[{region.Offset}], End=[{region.End}]. Total size: [{region.Size}] bytes.");
            }
        }
        Console.ResetColor();
    }
    
#endif

    private class Lease : IVkMemoryLease
    {
        public IVkMemory Memory { get; private set; } = null!;
        public VkMemoryRegion Region { get; private set; }
        public ulong Alignment { get; }
        public IObservable<IVkMemoryLease> MutationStream { get; }
        

        private readonly Action<IVkMemoryLease> deallocationContext;

        public Lease(IVkMemory memory, VkMemoryRegion region, ulong alignment, Action<IVkMemoryLease> deallocationContext, ISubject<IVkMemoryLease> leaseMutationSubject)
        {
            UpdateMemory(memory);
            UpdateRegion(region);
            MutationStream = leaseMutationSubject.Where(x => x == this);
            Alignment = alignment;

            this.deallocationContext = deallocationContext;
        }

        public void UpdateMemory(IVkMemory memory)
        {
            Memory = memory;
        }

        public void UpdateRegion(VkMemoryRegion region)
        {
            Region = region;
        }
        
        public void Dispose()
        {
            deallocationContext.Invoke(this);
        }
    }
}