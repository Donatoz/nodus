using System.Diagnostics;
using Serilog;

namespace Nodus.FlowEngine;

/// <summary>
/// Represents a non-deterministic single-threaded flow producer.
/// The flow is built with the respect of each unit's effective successor which is obtained at runtime,
/// whereas the resolve logic sequence is dynamically altered on each flow unit visit.
///
/// <remarks>
/// The flow that this producer generates might be not optimized, while it
/// fully respects the runtime aspect of each unit, where each unit can resolve the dependencies at runtime,
/// as well as 
/// </remarks>
/// </summary>
public class ImmediateProducer : IFlowProducer
{
    private readonly ILogger logger;

    public ImmediateProducer(ILogger logger)
    {
        this.logger = logger;
    }
    
    public IFlowUnit BuildFlow(IFlowToken root)
    {
        var flow = new DynamicFlow(root, logger);

        return flow.GetUnit();
    }

    private class DynamicFlow : IFlow
    {
        private readonly IFlowToken rootToken;
        private readonly IList<IFlowUnit> cache;
        private readonly ILogger logger;
        
        public DynamicFlow(IFlowToken rootToken, ILogger logger)
        {
            this.rootToken = rootToken;
            this.logger = logger;
            cache = new List<IFlowUnit>();
        }

        public void Append(IFlowUnit unit) => cache.Add(unit);

        public IFlowUnit GetUnit()
        {
            return new FlowDelegate("Root", GetUnitContext);
        }

        private async Task GetUnitContext(CancellationToken ct)
        {
            try
            {
                await ProcessRoot(rootToken, ct);
            }
            catch (FlowException e)
            {
                logger.Error($"Exception caught at unit [{e.Unit?.UnitId ?? "Global"}]: {e.WrappedException.Message}");
            }
        }

        private async Task ProcessRoot(IFlowToken token, CancellationToken ct)
        {
            var current = token;

            while (current != null)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    await ProcessToken(current, ct);
                }
                finally
                {
                    current = current.Successor;
                }
            }
        }

        private async Task ProcessToken(IFlowToken token, CancellationToken ct)
        {
            IFlowUnit? unit = null;
            
            try
            {
                token.Resolve(this);

                foreach (var u in cache)
                {
                    ct.ThrowIfCancellationRequested();
                    unit = u;
                    await unit.Execute(ct);
                }
                
                cache.Clear();

                if (token.Children == null) return;
                
                foreach (var child in token.Children)
                {
                    await ProcessRoot(child, ct);
                }
            }
            catch (Exception e)
            {
                throw e as FlowException ?? new FlowException(unit, e);
            }
        }
    }

    private class FlowException : Exception
    {
        public IFlowUnit? Unit { get; }
        public Exception WrappedException { get; }

        public FlowException(IFlowUnit? unit, Exception wrappedException)
        {
            Unit = unit;
            WrappedException = wrappedException;
        }
    }
}