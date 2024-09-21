using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Meta;
using Silk.NET.Vulkan;
using Buffer = System.Buffer;

namespace Nodus.RenderEngine.Vulkan.Memory;

/// <summary>
/// Represents a virtual memory heap that operates on a fixed memory allocation of a predefined type and size.
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
    private bool IsHostVisible => Meta.MemoryProperties.HasFlag(MemoryPropertyFlags.HostVisibleBit);
    
    private readonly ConcurrentDictionary<ulong, Lease> leases;
    private readonly SortedSet<VkMemoryRegion> freeRegions;
    /// <summary>
    /// A buffer that holds the latest array representation of the free regions set.
    /// High-intensity workloads use this representation.
    /// </summary>
    private VkMemoryRegion[] freeRegionsBuffer = null!;

    private readonly Subject<IVkMemoryLease> leaseMutationSubject;
    private readonly IVkLogicalDevice device;
    private readonly IVkPhysicalDevice physicalDevice;
    private readonly IVkFragmentationAnalyzer fragmentationAnalyzer;
    private readonly IVkDefragmenter defragmenter;
    private readonly IVkHeapAnalyzer[] analyzers;
    
    private VkMemory memory;
    private int mappedLeases;
    private readonly object syncRoot;
    
    private unsafe void* memoryPtr;

    public VkFixedMemoryHeap(IVkContext vkContext, IVkLogicalDevice device, IVkPhysicalDevice physicalDevice, IVkMemoryHeapInfo meta) : base(vkContext)
    {
        this.device = device;
        this.physicalDevice = physicalDevice;
        leases = new ConcurrentDictionary<ulong, Lease>();
        
        freeRegions = new SortedSet<VkMemoryRegion>(Comparer<VkMemoryRegion>.Create(
            // Use region's offset as the comparison basis, as it less impact on the memory load.
            // Frag-evaluation basically happens more frequently than memory leasing.
            (a, b) => a.Offset.CompareTo(b.Offset))
        );
        leaseMutationSubject = new Subject<IVkMemoryLease>();
        
        fragmentationAnalyzer = meta.FragmentationAnalyzer ?? new VkEntropicFragmentationAnalyzer();
        defragmenter = meta.Defragmenter ?? new VkLinearDefragmenter();
        analyzers = meta.Analyzers ?? [new VkHeapDefragmentationAnalyzer()];

        Meta = meta;
        syncRoot = new object();
        
        memory = new VkMemory(Context, device, physicalDevice, Meta.MemoryProperties);
        AllocateMemory();
        AddDependency(memory);

        var initialFreeRegion = new VkMemoryRegion(0, Meta.Size);
        
        freeRegions.Add(initialFreeRegion);
        UpdateFreeRegionsBuffer();
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

            UpdateFreeRegionsBuffer();
        }
        
        var lease = new Lease(memory, annexedRegion, alignment, FreeLease, leaseMutationSubject, this);

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
        if (freeRegions.Count <= 1) return;
        
        var regions = freeRegionsBuffer;

        var mergedRegions = new List<VkMemoryRegion>();
        var current = regions[0];

        for (var i = 1; i < regions.Length; i++)
        {
            var next = regions[i];

            if (next.Offset - current.End == 1)
            {
                current = new VkMemoryRegion(current.Offset, current.Size + next.Size);
            }
            else
            {
                mergedRegions.Add(current);
                current = next;
            }
        }
        
        mergedRegions.Add(current);
        freeRegions.Clear();
        mergedRegions.ForEach(x => freeRegions.Add(x));

        UpdateFreeRegionsBuffer();
    }

    private void AnalyzeMemory()
    {
        analyzers.ForEach(x => InterpretAnalysisResult(x.RequestAnalysis(this)));
    }

    private void InterpretAnalysisResult(HeapAnalysisResult result)
    {
        if (result.HasFlag(HeapAnalysisResult.DefragmentationRequired))
        {
            Context.RenderServices.Dispatcher.Enqueue(Defragment);
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

    private void FreeLease(Lease lease)
    {
        lock (syncRoot)
        {
            leases.Remove(lease.Region.Offset, out _);
            freeRegions.Add(lease.Region);
            UpdateFreeRegionsBuffer();

            MergeSubsequentRegions();
            AnalyzeMemory();

            if (lease.IsMapped)
            {
                lease.Unmap();
            }
        }
    }

    private unsafe void RequestMemoryMapping()
    {
        Interlocked.Increment(ref mappedLeases);
        
        if (memoryPtr != null) return;
        
        MapMemory();
    }

    private void UnmapLease()
    {
        Interlocked.Decrement(ref mappedLeases);
        
        if (mappedLeases == 0)
        {
            UnmapMemory();
        }
    }
    
    private unsafe void MapMemory()
    {
        void* mappedMemory;
        
        Context.Api.MapMemory(device.WrappedDevice, memory.WrappedMemory!.Value, 0, Meta.Size, 0, &mappedMemory)
            .TryThrow("Failed to map heap memory.");

        memoryPtr = mappedMemory;
    }

    private unsafe void UnmapMemory()
    {
        if (memoryPtr != null)
        {
            Context.Api.UnmapMemory(device.WrappedDevice, memory.WrappedMemory!.Value);
            memoryPtr = null;
        }
    }

    private unsafe byte[] AcquireRegionData(VkMemoryRegion region)
    {
        if (region.End + 1 > Meta.Size)
        {
            throw new Exception("Failed to acquire region data: region out of memory range.");
        }
        
        var result = new byte[region.Size];
        
        if (IsHostVisible)
        {
            if (memoryPtr == null)
            {
                MapMemory();
            }
            
            for (var i = region.Offset; i < region.Size; i++)
            {
                result[i] = *((byte*)memoryPtr + i);
            }

            if (mappedLeases == 0)
            {
                UnmapMemory();
            }
        }
        else
        {
            throw new NotImplementedException();
        }

        return result;
    }

    
    private unsafe void TransferLeaseData(List<(IVkMemoryLease lease, ulong previousOffset, byte[] snapshot)> leaseStates)
    {
        if (IsHostVisible)
        {
            if (memoryPtr == null)
            {
                MapMemory();
            }
            
            foreach (var l in leaseStates)
            {
                for (var i = 0ul; i < l.lease.Region.Size; i++)
                {
                    *((byte*)memoryPtr + l.lease.Region.Offset + i) = l.snapshot[i];
                    *((byte*)memoryPtr + l.previousOffset + i) = 0;
                }
            
                leaseMutationSubject.OnNext(l.lease);
            }
            
            if (mappedLeases == 0)
            {
                UnmapMemory();
            }
        }
        else
        {
            throw new NotImplementedException();
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

        var leaseValues = leases.Values.ToArray();
        leases.Clear();

        var movedLeases = new List<(IVkMemoryLease lease, ulong previousOffset, byte[] snapshot)>();

        leaseValues.ForEach(x =>
        {
            var previousOffset = x.Region.Offset;
            x.UpdateRegion(realignedLeaseRegions.First(r => r.Size == x.Region.Size));
            leases[x.Region.Offset] = x;

            if (previousOffset != x.Region.Offset)
            {
                movedLeases.Add((x, previousOffset, AcquireRegionData(new VkMemoryRegion(previousOffset, x.Region.Size))));
            }
        });
        
        TransferLeaseData(movedLeases);
        
        lock (syncRoot)
        {
            freeRegions.Clear();
            freeRegions.Add(new VkMemoryRegion(realignedLeaseRegions[^1].End + 1, Meta.Size - realignedLeaseRegions[^1].End));
            UpdateFreeRegionsBuffer();
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
            return fragmentationAnalyzer.EvaluateFragmentation(freeRegionsBuffer, Meta.Size);
        }
    }
    
    public ulong GetOccupiedMemory()
    {
        lock (syncRoot)
        {
            var totalFreeSize = 0ul;

            for (var i = 0; i < freeRegionsBuffer.Length; i++)
            {
                totalFreeSize += freeRegionsBuffer[i].Size;
            }
            
            return Meta.Size - totalFreeSize;
        }
    }

    private void UpdateFreeRegionsBuffer()
    {
        freeRegionsBuffer = freeRegions.ToArray();
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
        
        public bool IsMapped { get; private set; }

        private readonly Action<Lease> deallocationContext;
        private readonly VkFixedMemoryHeap heap;

        public Lease(IVkMemory memory, VkMemoryRegion region, ulong alignment, Action<Lease> deallocationContext, ISubject<IVkMemoryLease> leaseMutationSubject, VkFixedMemoryHeap heap)
        {
            UpdateMemory(memory);
            UpdateRegion(region);
            MutationStream = leaseMutationSubject.Where(x => x == this);
            Alignment = alignment;

            this.deallocationContext = deallocationContext;
            this.heap = heap;
        }

        public void UpdateMemory(IVkMemory memory)
        {
            Memory = memory;
        }

        public void UpdateRegion(VkMemoryRegion region)
        {
            Region = region;
        }
        
        public void MapToHost()
        {
            if (IsMapped)
            {
                throw new Exception("Failed to map the lease: lease was already mapped.");
            }
            
            IsMapped = true;
            heap.RequestMemoryMapping();
        }

        public void Unmap()
        {
            if (!IsMapped)
            {
                throw new Exception("Failed to unmap the lease: lease was not mapped.");
            }
            
            IsMapped = false;
            heap.UnmapLease();
        }

        public unsafe void SetMappedData(nint dataPtr, ulong size, ulong offset)
        {
            ValidateRequestedRegion(size, offset);

            Buffer.MemoryCopy((void*)dataPtr, (byte*)((ulong)heap.memoryPtr + offset + Region.Offset), size, size);
        }

        public unsafe Span<T> GetMappedData<T>(ulong size, ulong offset) where T : unmanaged
        {
            ValidateRequestedRegion(size, offset);
            
            return new Span<T>((T*)((ulong)heap.memoryPtr + offset + Region.Offset), (int)(size / (ulong)sizeof(T)));
        }

        private void ValidateRequestedRegion(ulong size, ulong offset)
        {
            if (!IsMapped)
            {
                throw new Exception("Failed to access mapped memory: lease was not mapped.");
            }

            if (Region.Offset + offset + size - 1 > Region.End)
            {
                throw new Exception($"Failed to access mapped memory: lease region was exceeded. " +
                                    $"Requested size: {size} bytes at offset: {offset}. " +
                                    $"Total lease size: {Region.Size}.");
            }
        }

        public void Dispose()
        {
            deallocationContext.Invoke(this);
        }
    }
}