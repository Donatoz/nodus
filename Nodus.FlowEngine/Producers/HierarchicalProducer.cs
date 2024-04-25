using Serilog;

namespace Nodus.FlowEngine;

/// <summary>
/// Represents a deterministic single-threaded flow producer.
/// Each flow unit will be executed in the hierarchical order starting from given root.
///
/// The produced flow contains all the ordered tokens as their resolved states.
///
/// <remarks>
/// The flow that this producer generates is the most optimized variant, as each unit state is being cached and becomes
/// immutable at runtime, with the whole logic sequence predetermined. Provided that, each unit in such flow shall
/// not depend on the runtime dependency resolving.
/// </remarks>
/// </summary>
public class HierarchicalProducer : IFlowProducer
{
    private readonly ILogger logger;
    
    public HierarchicalProducer(ILogger logger)
    {
        this.logger = logger;
    }
    
    public IFlowUnit BuildFlow(IFlowToken root)
    {
        var flow = new OrderedFlow(logger);

        ProcessToken(root, flow);

        return flow.GetUnit();
    }

    private void ProcessToken(IFlowToken current, IFlow flow)
    {
        current.Resolve(flow);

        if (current.Children != null)
        {
            foreach (var child in current.Children)
            {
                ProcessToken(child, flow);
            
                if (child.Successor != null)
                {
                    ProcessToken(child.Successor, flow);
                }
            }
        }
        
        if (current.Successor != null)
        {
            ProcessToken(current.Successor, flow);
        }
    }
    
    private class OrderedFlow : IFlow
    {
        private readonly IList<IFlowUnit> buffer;
        private readonly ILogger logger;
        
        public OrderedFlow(ILogger logger)
        {
            buffer = new List<IFlowUnit>();
            this.logger = logger;
        }
        
        public void Append(IFlowUnit context)
        {
            buffer.Add(context);
        }

        public IFlowUnit GetUnit()
        {
            return new FlowDelegate("Root", async ct =>
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
                        logger.Error($"Exception caught at unit [{unit.UnitId}]: {e.Message}");
                        break;
                    }
                }
            });
        }
    }
}