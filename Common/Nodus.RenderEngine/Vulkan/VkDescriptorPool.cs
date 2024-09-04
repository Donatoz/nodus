using Nodus.Common;
using Nodus.RenderEngine.Vulkan.DI;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Meta;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan;

public interface IVkDescriptorPoolContext
{
    VkDescriptorInfo[] Descriptors { get; }
    IVkDescriptorWriter DescriptorWriter { get; }
    
    IVkDescriptorSetFactory? DescriptorFactory { get; }
}

public record VkDescriptorPoolContext(
    VkDescriptorInfo[] Descriptors,
    IVkDescriptorWriter DescriptorWriter,
    IVkDescriptorSetFactory? DescriptorFactory = null)
    : IVkDescriptorPoolContext;

public interface IVkDescriptorPool : IVkUnmanagedHook
{
    DescriptorPool WrappedPool { get; }
    
    void UpdateSets();
    DescriptorSet GetSet(int index);
}

public class VkDescriptorPool : VkObject, IVkDescriptorPool
{
    public DescriptorPool WrappedPool { get; }

    protected DescriptorSet[] Sets { get; private set; } = null!;

    private readonly IVkLogicalDevice device;
    private readonly IVkDescriptorPoolContext descriptorPoolContext;
    private readonly uint descriptorsCount;
    private readonly DescriptorSetLayout layout;
    
    public unsafe VkDescriptorPool(IVkContext vkContext, IVkLogicalDevice device, IVkDescriptorPoolContext descriptorPoolContext, 
        uint descriptorsCount, DescriptorSetLayout layout) : base(vkContext)
    {
        this.device = device;
        this.descriptorPoolContext = descriptorPoolContext;
        this.descriptorsCount = descriptorsCount;
        this.layout = layout;

        var descriptorFactory = descriptorPoolContext.DescriptorFactory ?? new VkDescriptorSetFactory();

        using var sizes = descriptorFactory
            .CreateSizes(descriptorPoolContext.Descriptors, descriptorsCount)
            .ToFixedArray();

        var info = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PoolSizeCount = sizes.Length,
            PPoolSizes = sizes.Data,
            MaxSets = descriptorsCount
        };

        Context.Api.CreateDescriptorPool(device.WrappedDevice, in info, null, out var pool)
            .TryThrow("Failed to create descriptor pool.");
        
        WrappedPool = pool;
        
        CreateSets();
    }

    private unsafe void CreateSets()
    {
        var layouts = Enumerable.Repeat(layout, (int)descriptorsCount).ToArray();
        
        Sets = new DescriptorSet[descriptorsCount];

        fixed (void* pSets = Sets, pLayouts = layouts)
        {
            var allocInfo = new DescriptorSetAllocateInfo
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = WrappedPool,
                DescriptorSetCount = descriptorsCount,
                PSetLayouts = (DescriptorSetLayout*)pLayouts
            };
            
            Context.Api.AllocateDescriptorSets(device.WrappedDevice, in allocInfo, (DescriptorSet*)pSets)
                .TryThrow("Failed to allocate descriptor sets.");
        }
    }

    public unsafe void UpdateSets()
    {
        for (var i = 0; i < descriptorsCount; i++)
        {
            var writeSets = descriptorPoolContext.DescriptorWriter.CreateWriteSets(Sets[i], i);

            fixed (WriteDescriptorSet* p = writeSets)
            {
                Context.Api.UpdateDescriptorSets(device.WrappedDevice, (uint)writeSets.Length, p, 0, null);
            }
        }
    }

    public DescriptorSet GetSet(int index)
    {
        if (Sets.Length <= index)
        {
            throw new IndexOutOfRangeException($"Failed to get descriptor set at index: {index}");
        }
        
        return Sets[index];
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            Context.Api.DestroyDescriptorPool(device.WrappedDevice, WrappedPool, null);
        }
        
        base.Dispose(disposing);
    }
}