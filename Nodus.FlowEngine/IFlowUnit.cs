namespace Nodus.FlowEngine;

public interface IFlowUnit
{
    Task Execute(CancellationToken ct = default);
}

public readonly struct FlowDelegate : IFlowUnit
{
    private readonly Func<CancellationToken, Task> contextFactory;

    public FlowDelegate(Func<CancellationToken, Task> contextFactory)
    {
        this.contextFactory = contextFactory;
    }

    public Task Execute(CancellationToken ct = default) => contextFactory.Invoke(ct);
}