namespace Nodus.FlowEngine;

public interface IFlow
{
    void Append(IFlowUnit unit);
    IFlowUnit GetUnit();
}