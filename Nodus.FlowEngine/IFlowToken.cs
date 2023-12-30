namespace Nodus.FlowEngine;

public interface IFlowToken
{
    IFlowToken? Predecessor { get; set; }
    IFlowToken? Successor { get; set; }

    void Resolve(IFlow flow);
}

public class EmptyToken : IFlowToken
{
    public IFlowToken? Predecessor { get; set; }
    public IFlowToken? Successor { get; set; }
    
    public void Resolve(IFlow flow) { }
}