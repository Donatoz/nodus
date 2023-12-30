using Nodus.FlowEngine;

namespace FlowEditor.Factories;

public interface IFlowProducerFactory
{
    IFlowProducer Create();
}

public class FlowProducerFactory : IFlowProducerFactory
{
    public IFlowProducer Create()
    {
        return new SingleThreadProducer();
    }
}