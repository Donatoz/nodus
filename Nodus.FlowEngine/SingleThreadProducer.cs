using System.Diagnostics;

namespace Nodus.FlowEngine;

public class SingleThreadProducer : IFlowProducer
{
    public IFlowUnit BuildFlow(IFlowToken root)
    {
        var current = root;
        var flow = new OrderedFlow();

        while (current != null)
        {
            current.Resolve(flow);
            current = current.Successor;
        }

        return flow.GetUnit();
    }
    
    private class OrderedFlow : IFlow
    {
        private readonly IList<IFlowUnit> buffer;

        public OrderedFlow()
        {
            buffer = new List<IFlowUnit>();
        }
        
        public void Append(IFlowUnit context)
        {
            buffer.Add(context);
        }

        public IFlowUnit GetUnit()
        {
            return new FlowDelegate(async ct =>
            {
                foreach (var unit in buffer)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        await unit.Execute(ct);
                    }
                    catch (Exception e)
                    {
                        Trace.WriteLine(e.Message);
                        Trace.WriteLine(e.StackTrace);
                    }
                }
            });
        }
    }
}