using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Nodus.RenderEngine.Vulkan.Sync;

/// <summary>
/// <para>
/// Represents a GPU work item that exposes its state via Vulkan synchronization primitives.
/// A task can depend on other tasks, introducing an explicit synchronization scheme.
/// </para>
/// <para>
/// A task typically has a combined-host execution behavior, meaning that some of its logic is executed on the host
/// CPU, while another portion is submitted to the GPU. Semaphore chains serve as a synchronization context for the GPU payload
/// of all subsequent tasks.
/// </para>
/// <para>
/// For instance, a task can execute the preparation work on the CPU immediately and submit the work
/// on the GPU providing the semaphores acquired from the synchronization chain.
/// </para>
/// </summary>
public interface IVkTask
{
    /// <summary>
    /// Pipeline stage flags that are used in queue synchronization scenarios.
    /// </summary>
    PipelineStageFlags WaitStageFlags { get; }
    /// <summary>
    /// A set of tasks that are required to be executed before this task.
    /// </summary>
    ISet<IVkTask> Dependencies { get; }
    
    /// <summary>
    /// Optional semaphore that is signaled whenever the task has completed its execution.
    /// </summary>
    IVkSemaphore? SignalSemaphore { get; set; }
    /// <summary>
    /// Optional fence that is signaled whenever the task has completed its execution.
    /// </summary>
    IVkFence? CompletionFence { get; set; }
    
    /// <summary>
    /// Execute the task using the provided semaphores and pipeline stage flags.
    /// </summary>
    /// <param name="waitSemaphores">Semaphores to be awaited.</param>
    /// <param name="waitStageFlags">Awaited pipeline stage flags.</param>
    /// <returns></returns>
    VkTaskResult Execute(Semaphore[]? waitSemaphores = null, PipelineStageFlags[]? waitStageFlags = null);
}

public enum VkTaskResult
{
    Success,
    Failure
}

public abstract record VkTaskBase : IVkTask
{
    public string? Name { get; set; }
    public PipelineStageFlags WaitStageFlags { get; }
    public ISet<IVkTask> Dependencies => dependencies;
    
    public IVkSemaphore? SignalSemaphore { get; set; }
    public IVkFence? CompletionFence { get; set; }
    
    private readonly HashSet<IVkTask> dependencies;
    
    protected VkTaskBase(PipelineStageFlags waitStageFlags)
    {
        WaitStageFlags = waitStageFlags;
        dependencies = [];
    }
    
    public abstract VkTaskResult Execute(Semaphore[]? waitSemaphores, PipelineStageFlags[]? waitStageFlags);

    public override string? ToString()
    {
        return Name ?? base.ToString();
    }
}

public static class VkTaskExtensions
{
    public static bool IsSuccess(this VkTaskResult result)
    {
        return result == VkTaskResult.Success;
    }
}