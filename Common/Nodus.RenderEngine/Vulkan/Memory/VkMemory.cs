using Nodus.RenderEngine.Vulkan.Extensions;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Memory;

public interface IVkMemory : IVkUnmanagedHook
{
    DeviceMemory? WrappedMemory { get; }

    void Allocate(ulong allocationSize, uint type);
}

public class VkMemory : VkObject, IVkMemory
{
    public DeviceMemory? WrappedMemory { get; private set; }

    protected IVkLogicalDevice Device { get; }
    protected PhysicalDevice PhysicalDevice { get; }
    protected MemoryPropertyFlags Properties { get; }

    public VkMemory(IVkContext vkContext, IVkLogicalDevice device, PhysicalDevice physicalDevice, MemoryPropertyFlags properties) : base(vkContext)
    {
        Device = device;
        PhysicalDevice = physicalDevice;
        Properties = properties;
    }

    public unsafe void Allocate(ulong allocationSize, uint type)
    {
        TryFreeAllocatedMemory();
        
        var mallocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = allocationSize,
            MemoryTypeIndex = FindMemoryType(type, Properties)
        };

        DeviceMemory memory;
        Context.Api.AllocateMemory(Device.WrappedDevice, in mallocInfo, null, &memory)
            .TryThrow("Failed to allocate device memory.");
        
        WrappedMemory = memory;
    }
    
    private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        Context.Api.GetPhysicalDeviceMemoryProperties(PhysicalDevice, out var props);

        for (var i = 0; i < props.MemoryTypeCount; i++)
        {
            var isFilterMatched = (typeFilter & (1 << i)) > 0;
            var arePropsAligned = (props.MemoryTypes[i].PropertyFlags & properties) == properties;
            
            if (isFilterMatched && arePropsAligned)
            {
                return (uint)i;
            }
        }

        throw new Exception("Failed to find memory type for the buffer.");
    }
    
    private unsafe void TryFreeAllocatedMemory()
    {
        if (WrappedMemory != null)
        {
            Context.Api.FreeMemory(Device.WrappedDevice, WrappedMemory.Value, null);
            WrappedMemory = null;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            TryFreeAllocatedMemory();
        }
        
        base.Dispose(disposing);
    }
}