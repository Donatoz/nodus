namespace Nodus.FlowEngine;

/// <summary>
/// Represents a container for the logical flow.
/// </summary>
public interface IFlow
{
    void Append(IFlowUnit unit);
    IFlowUnit GetUnit();
}