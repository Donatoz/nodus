using Nodus.DI.Factories;
using Nodus.RenderEngine.Vulkan.Meta;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.DI;

public interface IVkDescriptorSetFactory
{
    DescriptorSetLayoutBinding[] CreateLayoutBindings();
    DescriptorPoolSize[] CreateSizes(VkDescriptorInfo[] descriptors, uint count);
}

public class VkDescriptorSetFactory : IVkDescriptorSetFactory
{
    public IFactory<DescriptorSetLayoutBinding[]>? LayoutFactory { get; set; }
    public IFactory<VkDescriptorInfo[], uint, DescriptorPoolSize[]>? SizesFactory { get; set; }
    
    public DescriptorSetLayoutBinding[] CreateLayoutBindings()
    {
        return LayoutFactory?.Create() ??
        [
            new DescriptorSetLayoutBinding
            {
                Binding = 0,
                DescriptorType = DescriptorType.UniformBuffer,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.VertexBit
            },
            new DescriptorSetLayoutBinding
            {
                Binding = 1,
                DescriptorType = DescriptorType.CombinedImageSampler,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.FragmentBit,
                PImmutableSamplers = null
            }
        ];
    }
    
    public DescriptorPoolSize[] CreateSizes(VkDescriptorInfo[] descriptors, uint count)
    {
        return SizesFactory?.Create(descriptors, count) ?? descriptors.Select(x => new DescriptorPoolSize
        {
            Type = x.Type,
            DescriptorCount = count
        }).ToArray();
    }
}