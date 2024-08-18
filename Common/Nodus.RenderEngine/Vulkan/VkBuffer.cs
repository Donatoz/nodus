using System.Runtime.InteropServices;
using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Utility;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Nodus.RenderEngine.Vulkan;

public interface IVkBufferContext
{
    uint Size { get; }
    BufferUsageFlags Usage { get; }
    SharingMode SharingMode { get; }
    MemoryPropertyFlags MemoryProperties { get; }
}

public readonly struct VkBufferContext(uint size, BufferUsageFlags usage, SharingMode sharingMode, MemoryPropertyFlags memoryProperties) : IVkBufferContext
{
    public uint Size { get; } = size;
    public BufferUsageFlags Usage { get; } = usage;
    public SharingMode SharingMode { get; } = sharingMode;
    public MemoryPropertyFlags MemoryProperties { get; } = memoryProperties;
}

public interface IVkBuffer<T> : IVkUnmanagedHook where T : unmanaged
{
    Buffer WrappedBuffer { get; }

    void UpdateData(ReadOnlySpan<T> data);
    void Allocate();
    void CmdCopyTo(IVkBuffer<T> another, CommandBuffer commandBuffer, Queue queue, Fence? fence = null);
    void MapToHost(uint mapSize);
    void SetMappedData(ReadOnlySpan<T> data);
}

public unsafe class VkBuffer<T> : VkObject, IVkBuffer<T> where T : unmanaged
{
    public Buffer WrappedBuffer { get; }

    protected DeviceMemory? Memory { get; private set; }

    private readonly IVkLogicalDevice device;
    private readonly PhysicalDevice physicalDevice;
    private readonly IVkBufferContext bufferContext;
    // TODO: Map this to T*
    private void* mappedMemory;

    public VkBuffer(IVkContext vkContext, IVkLogicalDevice device, PhysicalDevice physicalDevice, IVkBufferContext bufferContext) : base(vkContext)
    {
        this.device = device;
        this.physicalDevice = physicalDevice;
        this.bufferContext = bufferContext;

        var createInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = (uint)(sizeof(T) * bufferContext.Size),
            Usage = bufferContext.Usage,
            SharingMode = bufferContext.SharingMode
        };

        Buffer buffer;
        Context.Api.CreateBuffer(device.WrappedDevice, in createInfo, null, &buffer)
            .TryThrow($"Failed to create buffer: {createInfo.Usage}, Size={createInfo.Size}");

        WrappedBuffer = buffer;
    }

    private uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        Context.Api.GetPhysicalDeviceMemoryProperties(physicalDevice, out var props);

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

    public void Allocate()
    {
        TryFreeAllocatedMemory();
        
        Context.Api.GetBufferMemoryRequirements(device.WrappedDevice, WrappedBuffer, out var requirements);
        
        var mallocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = requirements.Size,
            MemoryTypeIndex = FindMemoryType(requirements.MemoryTypeBits, bufferContext.MemoryProperties)
        };

        DeviceMemory memory;
        Context.Api.AllocateMemory(device.WrappedDevice, in mallocInfo, null, &memory)
            .TryThrow("Failed to allocate device memory.");
        
        Memory = memory;

        Context.Api.BindBufferMemory(device.WrappedDevice, WrappedBuffer, Memory.Value, 0);
    }

    public void MapToHost(uint mapSize)
    {
        if (Memory == null)
        {
            throw new Exception("Failed to map buffer memory: buffer was not allocated.");
        }
        
        void* mem;
        
        Context.Api.MapMemory(device.WrappedDevice, Memory.Value, 0, bufferContext.Size, 0, &mem)
            .TryThrow("Failed to map buffer memory.");

        mappedMemory = mem;
    }

    public void SetMappedData(ReadOnlySpan<T> data)
    {
        data.CopyTo(new Span<T>(mappedMemory, data.Length));
    }

    public void UpdateData(ReadOnlySpan<T> data)
    {
        if (Memory == null)
        {
            throw new Exception("Failed to update buffer data: memory was not allocated.");
        }
        
        void* bufferData;
        
        Context.Api.MapMemory(device.WrappedDevice, Memory.Value, 0, bufferContext.Size, 0, &bufferData)
            .TryThrow("Failed to map buffer memory.");
        
        data.CopyTo(new Span<T>(bufferData, data.Length));
        
        Context.Api.UnmapMemory(device.WrappedDevice, Memory.Value);
    }

    public void CmdCopyTo(IVkBuffer<T> another, CommandBuffer commandBuffer, Queue queue, Fence? fence = null)
    {
        commandBuffer.SubmitCommandToQueue(() =>
        {
            Context.Api.CmdCopyBuffer(commandBuffer, WrappedBuffer, another.WrappedBuffer, 1, GetCopyContext());
        }, Context, queue, fence);
    }

    protected void TryFreeAllocatedMemory()
    {
        if (Memory != null)
        {
            Context.Api.FreeMemory(device.WrappedDevice, Memory.Value, null);
        }
    }

    protected virtual BufferCopy GetCopyContext()
    {
        return new BufferCopy { Size = bufferContext.Size };
    }

    protected override void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            if (mappedMemory != null)
            {
                Context.Api.UnmapMemory(device.WrappedDevice, Memory!.Value);
            }
            
            Context.Api.DestroyBuffer(device.WrappedDevice, WrappedBuffer, null);
            TryFreeAllocatedMemory();
        }
        
        base.Dispose(disposing);
    }
}