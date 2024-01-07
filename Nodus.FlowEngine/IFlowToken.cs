namespace Nodus.FlowEngine;

public interface IFlowToken
{
    IFlowToken? Successor { get; set; }

    void Resolve(IFlow flow);
}

public sealed class EmptyToken : IFlowToken
{
    public IFlowToken? Successor { get; set; }
    
    public void Resolve(IFlow flow) { }
}