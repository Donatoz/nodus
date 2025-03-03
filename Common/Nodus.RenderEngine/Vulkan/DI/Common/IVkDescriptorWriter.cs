using Nodus.RenderEngine.Vulkan.Meta;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.DI;

/// <summary>
/// Represents an interface for writing Vulkan descriptor sets. </summary>
/// <summary/>
public interface IVkDescriptorWriter
{
    WriteDescriptorSet[] CreateWriteSets(DescriptorSet destinationSet, int setIndex);
    void UpdateTokens(IVkDescriptorWriteToken[] tokens);
}

public class VkDescriptorWriter : IVkDescriptorWriter
{
    public static VkDescriptorWriter Empty { get; } = new([]);
    
    private IVkDescriptorWriteToken[] writeTokens = null!;

    public VkDescriptorWriter(IVkDescriptorWriteToken[] writeTokens)
    {
        UpdateTokens(writeTokens);
    }

    public void UpdateTokens(IVkDescriptorWriteToken[] tokens)
    {
        writeTokens = tokens;
    }
    
    public WriteDescriptorSet[] CreateWriteSets(DescriptorSet destinationSet, int setIndex)
    {
        var sets = new WriteDescriptorSet[writeTokens.Length];

        for (var i = 0; i < writeTokens.Length; i++)
        {
            sets[i] = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = destinationSet
            };
            
            writeTokens[i].PopulateWriteSet(ref sets[i], setIndex);
        }

        return sets;
    }
}