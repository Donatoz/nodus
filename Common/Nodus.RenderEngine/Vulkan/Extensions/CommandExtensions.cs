using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Extensions;

public static class CommandExtensions
{
    public static unsafe void SubmitCommandToQueue(this CommandBuffer buffer, Action cmdContext, IVkContext context, Queue queue, Fence? fence = null)
    {
        var begin = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        context.Api.BeginCommandBuffer(buffer, in begin);
        
        cmdContext.Invoke();

        context.Api.EndCommandBuffer(buffer);

        var submit = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &buffer
        };

        context.Api.QueueSubmit(queue, 1, in submit, fence ?? default);

        if (fence == null)
        {
            context.Api.QueueWaitIdle(queue);
        }
    }
}