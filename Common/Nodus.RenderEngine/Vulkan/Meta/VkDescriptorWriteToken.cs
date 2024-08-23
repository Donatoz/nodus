using Nodus.Common;
using Nodus.Core.Extensions;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Nodus.RenderEngine.Vulkan.Meta;

/// <summary>
/// Represents a descriptor write token that is used to populate a write descriptor set.
/// </summary>
public interface IVkDescriptorWriteToken
{
    ///<summary>
    /// Populates the write descriptor set using the provided write tokens.
    /// </summary>
    /// <param name="set">The destination descriptor set to write into.</param>
    /// <param name="setIndex">The index of the destination descriptor set in the descriptor pool.</param>
    void PopulateWriteSet(ref WriteDescriptorSet set, int setIndex);
}

/// <summary>
/// Represents a base class for buffer write tokens.
/// </summary>
public abstract class VkBufferWriteTokenBase : IVkDescriptorWriteToken
{
    private readonly uint binding;
    private readonly uint arrayElement;

    protected VkBufferWriteTokenBase(uint binding, uint arrayElement)
    {
        this.binding = binding;
        this.arrayElement = arrayElement;
    }
    
    public virtual void PopulateWriteSet(ref WriteDescriptorSet set, int setIndex)
    {
        set.DstBinding = binding;
        set.DstArrayElement = arrayElement;
    }
}

/// <summary>
/// Represents a write token used to populate a uniform buffer descriptor.
/// </summary>
public unsafe class VkUniformBufferWriteToken : VkBufferWriteTokenBase, IDisposable
{
    private readonly UnmanagedContainer<DescriptorBufferInfo>[] bufferInfos;

    /// <summary>
    /// Create a new instance of <see cref="VkUniformBufferWriteToken"/>
    /// </summary>
    /// <param name="binding">Destination binding index.</param>
    /// <param name="arrayElement">Destination array element index.</param>
    /// <param name="buffer">Uniform buffer handle.</param>
    /// <param name="offsets">Uniform buffer offsets. Each one represents the starting index of the required data.
    /// Each offset shall be mapped to the descriptor set index.</param>
    /// <param name="range">Range of the data to take from the uniform buffer.</param>
    public VkUniformBufferWriteToken(uint binding, uint arrayElement, Buffer buffer, ulong[] offsets, ulong range) 
        : base(binding, arrayElement)
    {
        bufferInfos = new UnmanagedContainer<DescriptorBufferInfo>[offsets.Length];

        for (var i = 0; i < offsets.Length; i++)
        {
            bufferInfos[i] = new UnmanagedContainer<DescriptorBufferInfo>();
            bufferInfos[i].Data->Buffer = buffer;
            bufferInfos[i].Data->Offset = offsets[i];
            bufferInfos[i].Data->Range = range;
        }
    }
    
    public override void PopulateWriteSet(ref WriteDescriptorSet set, int setIndex)
    {
        base.PopulateWriteSet(ref set, setIndex);

        if (setIndex >= bufferInfos.Length)
        {
            throw new Exception($"Failed to populate write descriptor set at index: {setIndex}. Buffered information count: {bufferInfos.Length}");
        }
        
        set.DescriptorType = DescriptorType.UniformBuffer;
        set.DescriptorCount = 1;
        set.PBufferInfo = bufferInfos[setIndex].Data;
    }

    public void Dispose()
    {
        bufferInfos.DisposeAll();
    }
}

/// <summary>
/// Represents a write token for a combined image sampler descriptor.
/// </summary>
public unsafe class VkImageSamplerWriteToken : VkBufferWriteTokenBase, IDisposable
{
    private readonly UnmanagedContainer<DescriptorImageInfo> imageInfo;

    /// <summary>
    /// Create a new instance of <see cref="VkImageSamplerWriteToken"/>.
    /// </summary>
    /// <param name="binding">Destination binding index.</param>
    /// <param name="arrayElement">Destination array element index.</param>
    /// <param name="view">Target image view.</param>
    /// <param name="sampler">Target image sampler.</param>
    public VkImageSamplerWriteToken(uint binding, uint arrayElement, ImageView view, Sampler sampler) : base(binding, arrayElement)
    {
        imageInfo = new UnmanagedContainer<DescriptorImageInfo>();

        imageInfo.Data->ImageView = view;
        imageInfo.Data->Sampler = sampler;
        imageInfo.Data->ImageLayout = ImageLayout.ShaderReadOnlyOptimal;
    }

    public override void PopulateWriteSet(ref WriteDescriptorSet set, int setIndex)
    {
        base.PopulateWriteSet(ref set, setIndex);

        set.DescriptorType = DescriptorType.CombinedImageSampler;
        set.DescriptorCount = 1;
        set.PImageInfo = imageInfo.Data;
    }

    public void Dispose()
    {
        imageInfo.Dispose();
    }
}