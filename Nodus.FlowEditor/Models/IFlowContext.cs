using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FlowEditor.Models.Primitives;
using Nodus.Core.Extensions;
using Nodus.Core.Reactive;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace FlowEditor.Models;

public interface IFlowContext : INodeContext, IDisposable
{
    IReactiveProperty<IFlowResolveContext?> CurrentResolveContext { get; }
    DescriptionProvider? TryGetDescriptionProvider();
    
    IFlowToken GetFlowToken(GraphContext context, Connection? sourceConnection);
    IFlowPortModel? GetEffectiveSuccessionPort(GraphContext ctx);
}

public interface IFlowResolveContext
{
    bool IsResolved { get; }
    Connection? SourceConnection { get; }
}

public abstract class FlowContextBase : NodeContextBase<IFlowNodeModel>, IFlowContext
{
    private readonly MutableReactiveProperty<IFlowResolveContext?> currentResolveContext;
    
    public IReactiveProperty<IFlowResolveContext?> CurrentResolveContext => currentResolveContext;

    protected FlowContextBase()
    {
        currentResolveContext = new MutableReactiveProperty<IFlowResolveContext?>();    
    }

    public IFlowToken GetFlowToken(GraphContext context, Connection? sourceConnection)
    {
        return GetEffectiveFlowToken(new FlowTokenContainer((f, t) => AlterFlow(f, context, sourceConnection, t)), context);
    }

    public virtual IFlowPortModel? GetEffectiveSuccessionPort(GraphContext ctx) => null;
    protected virtual IFlowToken GetEffectiveFlowToken(IFlowToken original, GraphContext context) => original;

    private void AlterFlow(IFlow flow, GraphContext context, Connection? sourceConnection, IFlowToken currentToken)
    {
        flow.Append(new FlowDelegate("Flow Unit", ct => Task.Run(async () =>
        {
            ct.ThrowIfCancellationRequested();
            UpdateResolveContext(true, sourceConnection);
            
            await Resolve(context, currentToken, ct);

            var extensionUnits = Node?.ContextExtensions.Items.Select(x => x.CreateFlowUnit(context))
                .Where(x => x is not null);

            if (extensionUnits != null)
            {
                foreach (var unit in extensionUnits)
                {
                    await unit!.Execute(ct);
                }
            }
            
            await Task.Delay(500, ct);
        }, ct).ContinueWith(_ => UpdateResolveContext(false, sourceConnection))));
    }

    private void UpdateResolveContext(bool isResolved, Connection? sourceConnection)
    {
        currentResolveContext.SetValue(new ResolveContext(isResolved, sourceConnection));
    }

    protected virtual Task Resolve(GraphContext context, IFlowToken currentToken, CancellationToken ct) =>
        Task.CompletedTask;
    
    public override void Deserialize(NodeContextData data) { }

    public override NodeContextData? Serialize() => null;

    public DescriptionProvider? TryGetDescriptionProvider()
    {
        var descs = GetDescriptors();

        if (!descs.Any())
        {
            return null;
        }

        var d = new DescriptionProvider();

        descs.ForEach(x => d.AddDescriptor(x));

        return d;
    }

    protected virtual IEnumerable<ValueDescriptor> GetDescriptors()
    {
        yield break;
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        
        currentResolveContext.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private class FlowTokenContainer : IFlowToken
    {
        public IFlowToken? Successor { get; set; }
        public IList<IFlowToken>? Children { get; set; }
        public ImmutableArray<IFlowToken>? DescendantTokens { get; set; }

        private readonly Action<IFlow, IFlowToken> resolveContext;

        public FlowTokenContainer(Action<IFlow, IFlowToken> resolveContext)
        {
            this.resolveContext = resolveContext;
        }

        public void Resolve(IFlow flow)
        {
            resolveContext.Invoke(flow, this);
        }
    }
    
    private readonly struct ResolveContext : IFlowResolveContext
    {
        public bool IsResolved { get; }
        public Connection? SourceConnection { get; }

        public ResolveContext(bool isResolved, Connection? sourceConnection)
        {
            IsResolved = isResolved;
            SourceConnection = sourceConnection;
        }
    }
}