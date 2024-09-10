namespace Nodus.RenderEngine.Common;

/// <summary>
/// Represents a dispatcher that is responsible for dispatching work items to be executed in a rendering thread.
/// </summary>
public interface IRenderDispatcher
{
    /// <summary>
    /// Enqueues a work item to be processed by a rendering thread.
    /// The work item is executed outside the primary rendering process, ensuring the data consistency during the
    /// frame rendering.
    /// </summary>
    /// <param name="workItem">The work item to enqueue.</param>
    void Enqueue(Action workItem);
}