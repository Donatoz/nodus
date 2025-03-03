using Nodus.RenderEngine.Vulkan.Extensions;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Nodus.RenderEngine.Vulkan.Sync;

/// <summary>
/// Represents a task that submits its payload to the specified GPU queue using the provided command buffer.
/// </summary>
public sealed record VkCommandTask : VkTaskBase
{
    private readonly IVkContext vkContext;
    private readonly Queue targetQueue;
    private readonly CommandBuffer commandBuffer;
    private readonly Func<CommandBuffer, bool> context;

    public VkCommandTask(PipelineStageFlags waitStageFlags, IVkContext vkContext, Queue targetQueue, CommandBuffer commandBuffer, Func<CommandBuffer, bool> context) 
        : base(waitStageFlags)
    {
        this.vkContext = vkContext;
        this.targetQueue = targetQueue;
        this.commandBuffer = commandBuffer;
        this.context = context;
    }

    public override unsafe VkTaskResult Execute(Semaphore[]? waitSemaphores, PipelineStageFlags[]? waitStageFlags)
    {
        var buffer = commandBuffer;
        var signalSemaphore = CompletionSemaphore?.WrappedSemaphore ?? default;

        vkContext.Api.ResetCommandBuffer(buffer, CommandBufferResetFlags.None);
        var result = context.Invoke(buffer);

        if (!result)
        {
            return VkTaskResult.Failure;
        }
        
        fixed (Semaphore* pWaitSemaphores = waitSemaphores)
        fixed (PipelineStageFlags* pWaitStageFlags = waitStageFlags)
        {
            var submitInfo = new SubmitInfo
            {
                SType = StructureType.SubmitInfo,
                WaitSemaphoreCount = (uint)(waitSemaphores?.Length ?? 0),
                PWaitSemaphores = waitSemaphores != null ? pWaitSemaphores : null,
                PWaitDstStageMask = pWaitStageFlags,
                CommandBufferCount = 1,
                PCommandBuffers = &buffer,
                SignalSemaphoreCount = CompletionSemaphore != null ? 1u : 0u,
                PSignalSemaphores = &signalSemaphore
            };
            
            vkContext.Api.QueueSubmit(targetQueue, 1, &submitInfo, CompletionFence?.WrappedFence ?? default);
        }

        return VkTaskResult.Success;
    }
}