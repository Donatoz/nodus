using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Nodus.RenderEngine.Vulkan.Sync;

public sealed record VkTask : VkTaskBase
{
    public static VkTask CompleteTask { get; } = new(PipelineStageFlags.None, (_, _) => VkTaskResult.Success);
    
    private readonly Func<Semaphore[]?, PipelineStageFlags[]?, VkTaskResult> context;
    
    public VkTask(PipelineStageFlags waitStageFlags, Func<Semaphore[]?, PipelineStageFlags[]?, VkTaskResult> context) : base(waitStageFlags)
    {
        this.context = context;
    }

    public override VkTaskResult Execute(Semaphore[]? waitSemaphores, PipelineStageFlags[]? waitStageFlags)
    {
        return context.Invoke(waitSemaphores, waitStageFlags);
    }
}