namespace Nodus.FlowEngine;

public interface IFlowProducer
{
    IFlowUnit BuildFlow(IFlowToken root);
}