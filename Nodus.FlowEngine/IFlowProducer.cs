namespace Nodus.FlowEngine;

/// <summary>
/// Represents a flow producer that can build the root flow unit based on a given root token.
/// </summary>
public interface IFlowProducer
{
    IFlowUnit BuildFlow(IFlowToken root);
}