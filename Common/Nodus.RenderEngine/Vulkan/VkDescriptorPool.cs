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
    
    void UpdateSets(WriteDescriptorSet[]? writeSets = null);
    void UpdateSet(int index, IVkDescriptorWriteToken writeToken);
    DescriptorSet GetSet(int index);
}

public class VkDescriptorPool : VkObject, IVkDescriptorPool
{
    public DescriptorPool WrappedPool { get; }

    protected DescriptorSet[] Sets { get; private set; } = null!;

    private readonly IVkLogicalDevice device;
    private readonly IVkDescriptorPoolContext descriptorPoolContext;
    private readonly uint maxSets;
    private readonly DescriptorSetLayout layout;
    
    public unsafe VkDescriptorPool(IVkContext vkContext, IVkLogicalDevice device, IVkDescriptorPoolContext descriptorPoolContext, 
        uint maxSets, DescriptorSetLayout layout) : base(vkContext)
    {
        this.device = device;
        this.descriptorPoolContext = descriptorPoolContext;
        this.maxSets = maxSets;
        this.layout = layout;

        var descriptorFactory = descriptorPoolContext.DescriptorFactory ?? new VkDescriptorSetFactory();

        var sizes = descriptorFactory.CreateSizes(descriptorPoolContext.Descriptors);

        fixed (DescriptorPoolSize* p = sizes)
        {
            var info = new DescriptorPoolCreateInfo
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = (uint)sizes.Length,
                PPoolSizes = p,
                MaxSets = maxSets
            };
            
            Context.Api.CreateDescriptorPool(device.WrappedDevice, in info, null, out var pool)
                .TryThrow("Failed to create descriptor pool.");
            
            WrappedPool = pool;
        }
        
        CreateSets();
    }

    private unsafe void CreateSets()
    {
        // TODO: Get rid of multi-buffer rendering approach, this functionality has to stay flexible and provide
        // non-uniform layouts option
        var layouts = Enumerable.Repeat(layout, (int)maxSets).ToArray();
        
        Sets = new DescriptorSet[maxSets];

        fixed (DescriptorSet* pSets = Sets)
        fixed (DescriptorSetLayout* pLayouts = layouts)
        {
            var allocInfo = new DescriptorSetAllocateInfo
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = WrappedPool,
                DescriptorSetCount = maxSets,
                PSetLayouts = pLayouts
            };
            
            Context.Api.AllocateDescriptorSets(device.WrappedDevice, in allocInfo, pSets)
                .TryThrow("Failed to allocate descriptor sets.");
        }
    }

    public unsafe void UpdateSets(WriteDescriptorSet[]? writeSets = null)
    {
        for (var i = 0; i < maxSets; i++)
        {
            var effectiveSets = writeSets ?? descriptorPoolContext.DescriptorWriter.CreateWriteSets(Sets[i], i);

            fixed (WriteDescriptorSet* p = effectiveSets)
            {
                Context.Api.UpdateDescriptorSets(device.WrappedDevice, (uint)effectiveSets.Length, p, 0, null);
            }
        }
    }

    public unsafe void UpdateSet(int index, IVkDescriptorWriteToken writeToken)
    {
        var set = GetSet(index);
        
        var write = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = set
        };
        
        writeToken.PopulateWriteSet(ref write, index);
        
        Context.Api.UpdateDescriptorSets(device.WrappedDevice, 1, &write, 0, null);
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