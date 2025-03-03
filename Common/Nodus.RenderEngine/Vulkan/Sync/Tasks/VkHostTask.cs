using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Nodus.RenderEngine.Vulkan.Sync;

/// <summary>
/// A task that encapsulates CPU-bound workload.
/// </summary>
public sealed record VkHostTask : VkTaskBase
{
    private readonly Func<Semaphore[]?, PipelineStageFlags[]?, VkTaskResult> context;
    
    public VkHostTask(PipelineStageFlags waitStageFlags, Func<Semaphore[]?, PipelineStageFlags[]?, VkTaskResult> context) : base(waitStageFlags)
    {
        this.context = context;
    }

    public VkHostTask(PipelineStageFlags waitStageFlags, Func<VkTaskResult> context) : this(waitStageFlags, (_, _) => context())
    {
    }

    public override VkTaskResult Execute(Semaphore[]? waitSemaphores, PipelineStageFlags[]? waitStageFlags)
    {
        return context.Invoke(waitSemaphores, waitStageFlags);
    }

    public static VkHostTask CreateCompleteTask()
    {
        return new VkHostTask(PipelineStageFlags.None, (_, _) => VkTaskResult.Success);
    }
}