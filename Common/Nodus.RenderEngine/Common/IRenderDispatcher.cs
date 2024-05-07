namespace Nodus.RenderEngine.Common;

public interface IRenderDispatcher
{
    void Enqueue(Action workItem, RenderWorkPriority priority);
}

public enum RenderWorkPriority
{
    Low,
    Medium,
    High
}