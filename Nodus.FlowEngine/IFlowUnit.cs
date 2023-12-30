namespace Nodus.FlowEngine;

public interface IFlowUnit
{
    Task GetContext();
}

public readonly struct FlowDelegate : IFlowUnit
{
    private readonly Func<Task> contextFactory;

    public FlowDelegate(Func<Task> contextFactory)
    {
        this.contextFactory = contextFactory;
    }

    public Task GetContext() => contextFactory.Invoke();
}