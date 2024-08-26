using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Meta;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Memory;

public interface IVkMemory : IVkUnmanagedHook
{
    DeviceMemory? WrappedMemory { get; }
    
    void Allocate(ulong allocationSize, uint type);
    T[] GetSnapshot<T>(uint size) where T : unmanaged;
}

public class VkMemory : VkObject, IVkMemory
{
    public DeviceMemory? WrappedMemory { get; private set; }

    protected IVkLogicalDevice Device { get; }
    protected IVkPhysicalDevice PhysicalDevice { get; }
    protected MemoryPropertyFlags Properties { get; }

    public VkMemory(IVkContext vkContext, IVkLogicalDevice device, IVkPhysicalDevice physicalDevice, MemoryPropertyFlags properties) : base(vkContext)
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

    public unsafe T[] GetSnapshot<T>(uint size) where T : unmanaged
    {
        if (!this.IsAllocated())
        {
            throw new Exception("Failed to get memory data: memory was not allocated.");
        }
        
        void* bufferData;
        
        Context.Api.MapMemory(Device.WrappedDevice, WrappedMemory!.Value, 0, size, 0, &bufferData)
            .TryThrow("Failed to map buffer memory.");

        var result = new Span<T>(bufferData, (int)size).ToArray();
        
        Context.Api.UnmapMemory(Device.WrappedDevice, WrappedMemory.Value);

        return result;
    }
    
    private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        Context.Api.GetPhysicalDeviceMemoryProperties(PhysicalDevice.WrappedDevice, out var props);

        for (var i = 0; i < props.MemoryTypeCount; i++)
        {
            var isFilterMatched = (typeFilter & (1 << i)) > 0;
            var arePropsAligned = (props.MemoryTypes[i].PropertyFlags & properties) == properties;
            
            if (isFilterMatched && arePropsAligned)
            {
                return (uint)i;
            }
        }

        throw new Exception("Failed to find memory type.");
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