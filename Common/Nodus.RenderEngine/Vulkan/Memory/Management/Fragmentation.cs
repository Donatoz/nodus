namespace Nodus.RenderEngine.Vulkan.Memory;

public interface IVkFragmentationAnalyzer
{
    double EvaluateFragmentation(IReadOnlyList<VkMemoryRegion> offsetOrderedRegions, ulong memorySize);
}

public interface IVkDefragmenter
{
    VkMemoryRegion[] Defragment(IEnumerable<(VkMemoryRegion region, ulong alignment)> regions);
}

/// <summary>
/// The entropic analyzer evaluates the total memory fragmentation based on the cumulative free space ratio and free regions
/// scattering across the memory allocation. In cases of high memory pressure, the analyzer shifts the weights towards entropy
/// relying on free space to a lesser extent.
/// </summary>
public class VkEntropicFragmentationAnalyzer : IVkFragmentationAnalyzer
{
    private readonly double entropyRatio;
    private readonly double maxEntropyRatio;
    private readonly double entropyMemoryThreshold;
    
    /// <summary>
    /// Create a new instance of <see cref="VkEntropicFragmentationAnalyzer"/>.
    /// </summary>
    /// <param name="entropyRatio">How much does entropy affect the fragmentation value?</param>
    /// <param name="maxEntropyRatio">Maximum entropy ratio achieved with the memory pressure.</param>
    /// <param name="entropyMemoryThreshold">Minimal occupied space ratio to shift the entropy weight.</param>
    public VkEntropicFragmentationAnalyzer(double entropyRatio = 0.4f, double maxEntropyRatio = 0.9, double entropyMemoryThreshold = 0.5)
    {
        this.entropyRatio = entropyRatio;
        this.maxEntropyRatio = maxEntropyRatio;
        this.entropyMemoryThreshold = entropyMemoryThreshold;
    }
    
    public double EvaluateFragmentation(IReadOnlyList<VkMemoryRegion> offsetOrderedRegions, ulong memorySize)
    {
        var effectiveEntropyRatio = entropyRatio;
        var orderedRegions = offsetOrderedRegions;
        var cumulativeFrag = 0.0;

        // Calculate the cumulative free space of non-contiguous free regions.
        for (var i = 0; i < orderedRegions.Count - 1; i++)
        {
            if (orderedRegions[i + 1].Offset - orderedRegions[i].Offset != 1)
            {
                cumulativeFrag += orderedRegions[i].Size;
            }
        }

        var totalFreeSize = 0ul;

        for (var i = 0; i < orderedRegions.Count; i++)
        {
            totalFreeSize += orderedRegions[i].Size;
        }

        // Try to shift the entropy weight according to the memory threshold.
        // The entropy weight is being shifted if there is enough occupied memory.
        var entropyShiftDelta = double.Max(0, (memorySize - totalFreeSize - memorySize * entropyMemoryThreshold) / (memorySize * entropyMemoryThreshold));
        effectiveEntropyRatio = double.Lerp(effectiveEntropyRatio, maxEntropyRatio, entropyShiftDelta);
        
        // Calculate the total entropy value of the regions based on their size (using Shannon's entropy equation).
        var entropy = 0.0;
        for (var i = 0; i < orderedRegions.Count; i++)
        {
            var p = orderedRegions[i].Size / totalFreeSize;
            entropy -= p * Math.Log2(p);
        }

        // Normalize the entropy value and cumulative size.
        var normalizedEntropy = entropy > 0 ? entropy / Math.Log2(orderedRegions.Count) : 0;
        cumulativeFrag /= memorySize;

        var result = (1.0 - effectiveEntropyRatio) * cumulativeFrag + effectiveEntropyRatio * normalizedEntropy;

        return result;
    }
}

// TODO: Minor feature
// Implement weighted frag analyzer based on giving more weight to the free memory regions that are close enough to
// place a commonly-sized object.
public class VkWeightedFragmentationAnalyzer : IVkFragmentationAnalyzer
{
    public double EvaluateFragmentation(IReadOnlyList<VkMemoryRegion> offsetOrderedRegions, ulong memorySize)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// The linear defragmenter completely redefines memory regions placement, most probably changing each region offset.
/// </summary>
public class VkLinearDefragmenter : IVkDefragmenter
{
    // TODO: Implement partial defragmentation, so that critical memory regions would be pinned during defragmentation.
    public VkMemoryRegion[] Defragment(IEnumerable<(VkMemoryRegion region, ulong alignment)> regions)
    {
        var orderedRegions = regions.OrderBy(region => region.region.Offset).ToArray();
        var rearrangedRegions = new VkMemoryRegion[orderedRegions.Length];

        var currentOffset = 0ul;
        for (var i = 0; i < orderedRegions.Length; i++)
        {
            var reg = orderedRegions[i];
            var alignedOffset = (currentOffset + reg.alignment - 1) & ~(reg.alignment - 1);
            
            rearrangedRegions[i] = new VkMemoryRegion(alignedOffset, reg.region.Size);

            currentOffset = rearrangedRegions[i].End + 1;
        }

        return rearrangedRegions;
    }
}