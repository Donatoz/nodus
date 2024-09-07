namespace Nodus.RenderEngine.Vulkan.Memory;

public interface IVkHeapAnalyzer
{
    HeapAnalysisResult RequestAnalysis(IVkMemoryHeap heap);
}

public enum HeapAnalysisResult
{
    NoChangeRequired,
    DefragmentationRequired
}

/// <summary>
/// The fragmentation analyzer checks whether the heap current state is a subject for defragmentation and gives a signal
/// for the defragmentation, if required.
/// </summary>
public sealed class VkHeapDefragmentationAnalyzer : IVkHeapAnalyzer
{
    private readonly float defragmentationThreshold;
    
    public VkHeapDefragmentationAnalyzer(float defragmentationThreshold = 0.75f)
    {
        this.defragmentationThreshold = defragmentationThreshold;
    }
    
    public HeapAnalysisResult RequestAnalysis(IVkMemoryHeap heap)
    {
        var fragmentationValue = heap.GetCurrentFragmentation();

        return fragmentationValue >= defragmentationThreshold 
            ? HeapAnalysisResult.DefragmentationRequired 
            : HeapAnalysisResult.NoChangeRequired;
    }
}