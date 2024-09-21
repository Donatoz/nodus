using Nodus.Core.Extensions;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Nodus.RenderEngine.Vulkan.Sync;

/// <summary></summary>
/// Represents a task graph for executing GPU work items.
/// A task graph manages a collection of tasks and their dependencies, and provides methods to execute and manipulate them.
public interface IVkTaskGraph : IVkUnmanagedHook
{
    ///<summary>
    /// Execute the task graph according to the <see cref="VkTaskGraphExecutionStrategy"/>.
    /// </summary>
    /// <exception cref="System.Exception">Thrown if the graph is not baked prior to execution.</exception>
    void Execute();

    /// <summary>
    /// Execute the bake process for the task graph.
    /// </summary>
    /// <remarks>
    /// The bake process involves sorting the tasks based on their dependencies, creating a submission payload for each task,
    /// and setting the awaited semaphores and stages for each submission payload token.
    /// </remarks>
    void Bake();

    /// <summary>
    /// Add a new task to the task graph.
    /// </summary>
    /// <param name="task">The task to be added.</param>
    void AddTask(IVkTask task);

    /// <summary>
    /// Remove the specified task from the task graph.
    /// </summary>
    /// <param name="task">The task to remove.</param>
    void RemoveTask(IVkTask task);
}

public enum VkTaskGraphExecutionStrategy
{
    Subsequent,
    Parallel
}

public class VkTaskGraph : VkObject, IVkTaskGraph
{
    private readonly VkTaskGraphExecutionStrategy executionStrategy;
    private readonly bool stopOnFail;
    private readonly HashSet<IVkTask> tasks;

    private IList<SubmissionPayloadToken>? submissionPayload;

    /// <summary>
    /// Create a new instance of <see cref="VkTaskGraph"/>
    /// </summary>
    /// <param name="vkContext">Vulkan Context.</param>
    /// <param name="executionStrategy">Graph tasks execution strategy</param>
    /// <param name="stopOnFail">If set to true - the execution will stop whenever the currently executed task returns <see cref="VkTaskResult.Failure"/>.</param>
    public VkTaskGraph(IVkContext vkContext, VkTaskGraphExecutionStrategy executionStrategy = VkTaskGraphExecutionStrategy.Parallel, bool stopOnFail = true) : base(vkContext)
    {
        this.executionStrategy = executionStrategy;
        this.stopOnFail = stopOnFail;
        tasks = [];
    }
    
    public void Execute()
    {
        if (submissionPayload == null)
        {
            throw new Exception("Failed to submit tasks: graph must be baked prior to the submission.");
        }

        if (submissionPayload.Count == 0) return;

        if (executionStrategy == VkTaskGraphExecutionStrategy.Subsequent)
        {
            foreach (var t in submissionPayload)
            {
                if (!ExecutePayloadToken(t).IsSuccess() && stopOnFail)
                {
                    break;
                }
            }
        }
        else
        {
            Parallel.ForEach(submissionPayload, t => ExecutePayloadToken(t));
        }
    }

    private VkTaskResult ExecutePayloadToken(SubmissionPayloadToken token)
    {
        token.Task.CompletionFence?.Reset();
        return token.Task.Execute(token.AwaitedSemaphores, token.WaitStages);
    }

    public void Bake()
    {
        var sortedTasks = GetSortedTasks();
        
        submissionPayload = new List<SubmissionPayloadToken>();

        for (var i = 0; i < sortedTasks.Count; i++)
        {
            submissionPayload.Add(new SubmissionPayloadToken
            {
                Task = sortedTasks[i],
                Semaphore = sortedTasks[i].SignalSemaphore,
                Order = i
            });
        }

        foreach (var token in submissionPayload)
        {
            if (token.Task.Dependencies.Count == 0) continue;
            
            var awaitedSemaphores = new List<Semaphore>();
            var awaitedStages = new HashSet<PipelineStageFlags>();

            foreach (var depTask in token.Task.Dependencies)
            {
                var task = depTask;
                var depToken = submissionPayload.First(x => x.Task == task);

                if (depToken.Semaphore != null)
                {
                    awaitedSemaphores.Add(depToken.Semaphore.WrappedSemaphore);
                }
                awaitedStages.Add(depTask.WaitStageFlags);
            }

            token.AwaitedSemaphores = awaitedSemaphores.ToArray();
            token.WaitStages = awaitedStages.ToArray();
        }
    }

    private IList<IVkTask> GetSortedTasks()
    {
        var visited = new HashSet<IVkTask>();
        var visiting = new HashSet<IVkTask>();
        var result = new List<IVkTask>();
        
        foreach (var task in tasks)
        {
            if (visited.Contains(task)) continue;
            
            if (!SortTasks(task, visited, visiting, result))
            {
                throw new Exception("Failed to sort tasks: graph must be acyclic.");
            }
        }
        
        return result;
    }
    
    private bool SortTasks(IVkTask task, HashSet<IVkTask> visited, HashSet<IVkTask> visiting, List<IVkTask> result)
    {
        visiting.Add(task);

        foreach (var dep in task.Dependencies)
        {
            if (visiting.Contains(dep))
            {
                return false;
            }

            if (!visited.Contains(dep))
            {
                if (!SortTasks(dep, visited, visiting, result))
                {
                    return false;
                }
            }
        }

        visiting.Remove(task);
        visited.Add(task);
        result.Add(task);
        return true;
    }
    
    public void AddTask(IVkTask task)
    {
        tasks.Add(task);
    }

    public void RemoveTask(IVkTask task)
    {
        tasks.Remove(task);
    }

    private class SubmissionPayloadToken
    {
        public required IVkTask Task { get; init; }
        public required int Order { get; init; }
        public IVkSemaphore? Semaphore { get; init; }
        
        public Semaphore[]? AwaitedSemaphores { get; set; }
        public PipelineStageFlags[]? WaitStages { get; set; }
    }
}