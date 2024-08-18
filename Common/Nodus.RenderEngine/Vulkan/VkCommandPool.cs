using Nodus.RenderEngine.Vulkan.Extensions;
using Nodus.RenderEngine.Vulkan.Meta;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan;

public interface IVkCommandPool : IVkUnmanagedHook
{
    CommandPool WrappedPool { get; }
    
    void Begin(int buffer);
    void End(int buffer);
    void Reset(int buffer, CommandBufferResetFlags flags);
    CommandBuffer GetBuffer(int index);
}

public class VkCommandPool : VkObject, IVkCommandPool
{
    public CommandPool WrappedPool { get; }

    protected CommandBuffer[] WrappedBuffers { get; }

    private readonly IVkLogicalDevice device;
    
    public unsafe VkCommandPool(IVkContext vkContext, IVkLogicalDevice device, VkQueueInfo queueInfo, uint bufferCount, CommandPoolCreateFlags flags) : base(vkContext)
    {
        this.device = device;
        queueInfo.ThrowIfIncomplete();

        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            Flags = flags,
            QueueFamilyIndex = queueInfo.GraphicsFamily!.Value
        };

        Context.Api.CreateCommandPool(device.WrappedDevice, in poolInfo, null, out var pool)
            .TryThrow("Failed to create command pool.");

        WrappedPool = pool;

        var bufferAllocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = WrappedPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = bufferCount
        };

        WrappedBuffers = new CommandBuffer[bufferCount];

        fixed (CommandBuffer* p = WrappedBuffers)
        {
            Context.Api.AllocateCommandBuffers(device.WrappedDevice, in bufferAllocInfo, p)
                .TryThrow("Failed to allocate cmd buffer.");
        }
    }

    public void Begin(int buffer)
    {
        var begin = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo
        };
        
        Context.Api.BeginCommandBuffer(GetBuffer(buffer), in begin)
            .TryThrow("Failed to begin the command buffer.");
    }

    public void End(int buffer)
    {
        Context.Api.EndCommandBuffer(GetBuffer(buffer))
            .TryThrow("Failed to end the command buffer.");
    }
    
    public void Reset(int buffer, CommandBufferResetFlags flags)
    {
        Context.Api.ResetCommandBuffer(GetBuffer(buffer), flags)
            .TryThrow("Failed to reset command buffer.");
    }

    public CommandBuffer GetBuffer(int index)
    {
        if (WrappedBuffers.Length <= index)
        {
            throw new IndexOutOfRangeException($"Failed to get command buffer at index: {index}");
        }

        return WrappedBuffers[index];
    }

    protected override unsafe void Dispose(bool disposing)
    {
        if (IsDisposing)
        {
            Context.Api.DestroyCommandPool(device.WrappedDevice, WrappedPool, null);
        }
        
        base.Dispose(disposing);
    }
}