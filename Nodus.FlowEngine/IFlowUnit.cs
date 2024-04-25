namespace Nodus.FlowEngine;

/// <summary>
/// Represents a fundamental flow unit.
/// </summary>
public interface IFlowUnit
{
    /// <summary>
    /// Non-unique identifier of the unit.
    /// </summary>
    string UnitId { get; }
    Task Execute(CancellationToken ct = default);
}

public readonly struct FlowDelegate : IFlowUnit
{
    public string UnitId { get; }

    private readonly Func<CancellationToken, Task> contextFactory;

    public FlowDelegate(string id, Func<CancellationToken, Task> contextFactory)
    {
        UnitId = id;
        this.contextFactory = contextFactory;
    }

    public Task Execute(CancellationToken ct = default) => contextFactory.Invoke(ct);
}

public readonly struct AnonymousFlowDelegate : IFlowUnit
{
    public string UnitId => string.Empty;

    private readonly Action context;

    public AnonymousFlowDelegate(Action context)
    {
        this.context = context;
    }
    
    public Task Execute(CancellationToken ct = default)
    {
        context.Invoke();
        return Task.CompletedTask;
    }
}