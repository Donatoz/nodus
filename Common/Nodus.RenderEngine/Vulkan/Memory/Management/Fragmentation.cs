namespace Nodus.RenderEngine.Vulkan.Memory;

public interface IVkDefragmenter
{
    float EvaluateFragmentation(IReadOnlyCollection<VkMemoryRegion> regions, uint memorySize);
    void Defragment(ICollection<VkMemoryRegion> regions, uint memorySize);
}