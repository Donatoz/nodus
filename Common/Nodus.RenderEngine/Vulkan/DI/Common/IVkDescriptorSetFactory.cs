using Nodus.Common;
using Nodus.DI.Factories;
using Nodus.RenderEngine.Vulkan.Meta;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.DI;

public interface IVkDescriptorSetFactory
{
    DescriptorSetLayoutBinding[] CreateLayoutBindings();
    DescriptorPoolSize[] CreateSizes(VkDescriptorInfo[] descriptors);
    DescriptorSetLayoutCreateInfo[] CreateDescriptorSetLayouts(IFixedEnumerable<DescriptorSetLayoutBinding> bindings);
    PushConstantRange[] CreatePushConstants();
}

public class VkDescriptorSetFactory : IVkDescriptorSetFactory
{
    public static VkDescriptorSetFactory DefaultDescriptorSetFactory { get; } = new();
    
    public Func<DescriptorSetLayoutBinding[]>? BindingsFactory { get; set; }
    public Func<VkDescriptorInfo[], DescriptorPoolSize[]>? SizesFactory { get; set; }
    public Func<IFixedEnumerable<DescriptorSetLayoutBinding>, DescriptorSetLayoutCreateInfo[]>? LayoutFactory { get; set; }
    public Func<PushConstantRange[]>? PushConstantsFactory { get; set; }
    
    public DescriptorSetLayoutBinding[] CreateLayoutBindings()
    {
        return BindingsFactory?.Invoke() ??
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
                StageFlags = ShaderStageFlags.FragmentBit | ShaderStageFlags.VertexBit,
                PImmutableSamplers = null
            }
        ];
    }
    
    public DescriptorPoolSize[] CreateSizes(VkDescriptorInfo[] descriptors)
    {
        return SizesFactory?.Invoke(descriptors) ?? descriptors.Select(x => new DescriptorPoolSize
        {
            Type = x.Type,
            DescriptorCount = x.Count
        }).ToArray();
    }

    public unsafe DescriptorSetLayoutCreateInfo[] CreateDescriptorSetLayouts(IFixedEnumerable<DescriptorSetLayoutBinding> bindings)
    {
        return LayoutFactory?.Invoke(bindings) ??
        [
            new DescriptorSetLayoutCreateInfo
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = bindings.Length,
                PBindings = bindings.Data
            }
        ];
    }

    public PushConstantRange[] CreatePushConstants()
    {
        return PushConstantsFactory?.Invoke() ?? [];
    }
}