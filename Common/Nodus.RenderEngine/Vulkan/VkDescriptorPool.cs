using Nodus.RenderEngine.Vulkan.Extensions;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Nodus.RenderEngine.Vulkan;

public interface IVkDescriptorPool : IVkUnmanagedHook
{
    DescriptorPool WrappedPool { get; }
    
    void PopulateDescriptors(Buffer buffer, uint bufferRegionSize, ulong[] offsets, uint binding = 0, uint arrayElement = 0);
    DescriptorSet GetSet(int index);
}

public class VkDescriptorPool : VkObject, IVkDescriptorPool
{
    public DescriptorPool WrappedPool { get; }

    protected DescriptorSet[] Sets { get; private set; } = null!;

    private readonly IVkLogicalDevice device;
    private readonly uint descriptorsCount;
    private readonly DescriptorSetLayout layout;
    private readonly DescriptorType type;
    
    public unsafe VkDescriptorPool(IVkContext vkContext, IVkLogicalDevice device, DescriptorType type, uint descriptorsCount, DescriptorSetLayout layout) : base(vkContext)
    {
        this.device = device;
        this.type = type;
        this.descriptorsCount = descriptorsCount;
        this.layout = layout;

        var size = new DescriptorPoolSize
        {
            Type = type,
            DescriptorCount = descriptorsCount
        };

        var info = new DescriptorPoolCreateInfo
        {
            SType = StructureType.DescriptorPoolCreateInfo,
            PoolSizeCount = 1,
            PPoolSizes = &size,
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

    public unsafe void PopulateDescriptors(Buffer buffer, uint bufferRegionSize, ulong[] offsets, uint binding = 0, uint arrayElement = 0)
    {
        if (offsets.Length < descriptorsCount)
        {
            throw new Exception(
                "Failed to populate descriptor sets: each descriptors has to be supplied with a buffer offset.");
        }
        
        for (var i = 0; i < descriptorsCount; i++)
        {
            var bufferInfo = new DescriptorBufferInfo
            {
                Buffer = buffer,
                Offset = offsets[i],
                Range = bufferRegionSize
            };

            var writeSet = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = Sets[i],
                DstBinding = binding,
                DstArrayElement = arrayElement,
                DescriptorType = type,
                DescriptorCount = 1,
                PBufferInfo = &bufferInfo
            };
            
            Context.Api.UpdateDescriptorSets(device.WrappedDevice, 1, &writeSet, 0, null);
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