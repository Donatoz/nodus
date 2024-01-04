using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Nodus.Core.Reactive;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace FlowEditor.Models;

public interface IFlowNodeContext : INodeContext, IDisposable
{
    public IReactiveProperty<bool> IsBeingResolved { get; }
    
    void Bind(IFlowNodeModel node);
    object? ResolvePortValue(IFlowPortModel port, GraphContext context);
    IFlowToken GetFlowToken(GraphContext context);
}

public abstract class FlowNodeContextBase : IFlowNodeContext
{
    private readonly IDictionary<IFlowPortModel, Func<object>> portValueBindings;
    private readonly MutableReactiveProperty<bool> isBeingResolved;
    protected IFlowNodeModel? Node { get; private set; }

    public IReactiveProperty<bool> IsBeingResolved => isBeingResolved;

    public FlowNodeContextBase()
    {
        portValueBindings = new Dictionary<IFlowPortModel, Func<object>>();
        isBeingResolved = new MutableReactiveProperty<bool>();
    }

    public virtual void Bind(IFlowNodeModel node)
    {
        Node = node;
    }
    
    public object? ResolvePortValue(IFlowPortModel port, GraphContext context)
    {
        return port.Type == PortType.Input ? context.GetInputPortValue(port) : GetOutputValue(port);
    }

    protected void BindPortValue(IFlowPortModel port, Func<object> valueGetter)
    {
        portValueBindings[port] = valueGetter;
    }

    protected object GetOutputValue(IFlowPortModel port)
    {
        if (!portValueBindings.ContainsKey(port))
        {
            throw new Exception($"Port ({port}) value is not bound to anything.");
        }

        return portValueBindings[port].Invoke();
    }

    public IFlowToken GetFlowToken(GraphContext context)
    {
        return new FlowTokenContainer(f => Resolve(f, context));
    }

    protected virtual void Resolve(IFlow flow, GraphContext context)
    {
        flow.Append(new FlowDelegate(() =>
        {
            isBeingResolved.SetValue(true);
            Task.Delay(1000).ContinueWith(_ => isBeingResolved.SetValue(false));
            
            return Task.CompletedTask;
        }));
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        
        isBeingResolved.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private class FlowTokenContainer : IFlowToken
    {
        public IFlowToken? Predecessor { get; set; }
        public IFlowToken? Successor { get; set; }

        private readonly Action<IFlow> resolveContext;

        public FlowTokenContainer(Action<IFlow> resolveContext)
        {
            this.resolveContext = resolveContext;
        }
        
        public void Resolve(IFlow flow)
        {
            resolveContext.Invoke(flow);
        }
    }
}