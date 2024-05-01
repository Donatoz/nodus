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
    DescriptionProvider? GetDescriptionProvider();
    
    void Bind(IFlowNodeModel node);
    object? ResolvePortValue(IFlowPortModel port, GraphContext context);
    IFlowToken GetFlowToken(GraphContext context, Connection? sourceConnection);
    IFlowPortModel? GetEffectiveSuccessionPort(GraphContext ctx);
}

public interface IFlowResolveContext
{
    bool IsResolved { get; }
    Connection? SourceConnection { get; }
}

public abstract class FlowContextBase : IFlowContext
{
    private readonly IDictionary<IFlowPortModel, Func<GraphContext, object?>> portValueBindings;
    private readonly MutableReactiveProperty<IFlowResolveContext?> currentResolveContext;

    protected IFlowNodeModel? Node { get; private set; }

    public IReactiveProperty<IFlowResolveContext?> CurrentResolveContext => currentResolveContext;

    protected FlowContextBase()
    {
        portValueBindings = new Dictionary<IFlowPortModel, Func<GraphContext, object?>>();
        currentResolveContext = new MutableReactiveProperty<IFlowResolveContext?>();    
    }

    public virtual void Bind(IFlowNodeModel node)
    {
        Node = node;
        ResetPortBindings();
    }
    
    public object? ResolvePortValue(IFlowPortModel port, GraphContext context)
    {
        return port.Type == PortType.Input ? context.GetInputPortValue(port) : GetOutputValue(port, context);
    }

    protected void BindPortValue(IFlowPortModel port, Func<GraphContext, object?> valueGetter)
    {
        portValueBindings[port] = valueGetter;
    }

    protected void TryBindFirstOutPort(Func<GraphContext, object?> valueGetter) => TryBindOutPort(0, valueGetter);

    protected void TryBindOutPort(int index, Func<GraphContext, object?> valueGetter)
    {
        var port = Node?.GetFlowPorts().Where(x => x.Type == PortType.Output).ElementAt(index);
        
        if (port == null) return;
        
        BindPortValue(port, valueGetter);
    }

    protected void ResetPortBindings()
    {
        portValueBindings.Clear();
    }

    protected object? GetOutputValue(IFlowPortModel port, GraphContext context)
    {
        if (!portValueBindings.ContainsKey(port))
        {
            throw new Exception($"Port ({port.Header}) value is not bound to anything.");
        }

        return portValueBindings[port].Invoke(context);
    }

    public IFlowToken GetFlowToken(GraphContext context, Connection? sourceConnection)
    {
        return GetEffectiveFlowToken(new FlowTokenContainer((f, t) => BeginResolve(f, context, sourceConnection, t)), context);
    }

    public virtual IFlowPortModel? GetEffectiveSuccessionPort(GraphContext ctx) => null;
    protected virtual IFlowToken GetEffectiveFlowToken(IFlowToken original, GraphContext context) => original;

    private void BeginResolve(IFlow flow, GraphContext context, Connection? sourceConnection, IFlowToken currentToken)
    {
        flow.Append(new FlowDelegate("Delay Unit", async ct =>
        {
            ct.ThrowIfCancellationRequested();
            UpdateResolveContext(true, sourceConnection);
            await Task.Delay(500, ct);
        }));
        
        AlterFlow(flow, context, currentToken);

        Node?.ContextExtensions.Items.Select(x => x.CreateFlowUnit(context))
            .Where(x => x is not null).ForEach(flow.Append!);
        
        flow.Append(new AnonymousFlowDelegate(() => UpdateResolveContext(false, sourceConnection)));
    }

    private void UpdateResolveContext(bool isResolved, Connection? sourceConnection)
    {
        currentResolveContext.SetValue(new ResolveContext(isResolved, sourceConnection));
    }

    protected virtual void AlterFlow(IFlow flow, GraphContext context, IFlowToken currentToken) { }
    
    public virtual void Deserialize(NodeContextData data) { }

    public virtual NodeContextData? Serialize() => null;

    public DescriptionProvider? GetDescriptionProvider()
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