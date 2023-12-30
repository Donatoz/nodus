using System;
using System.Threading.Tasks;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Meta;

namespace FlowEditor.Models.Contexts;

public sealed class GenericFlowContext : FlowNodeContextBase
{
    private readonly Action<IFlowNodeModel, GraphContext> resolveContext;
    
    public GenericFlowContext(Action<IFlowNodeModel, GraphContext> resolveContext)
    {
        this.resolveContext = resolveContext;
    }

    protected override void Resolve(IFlow flow, GraphContext context)
    {
        if (Node == null)
        {
            throw new Exception("Failed to resolve node context: node is not bound.");
        }
        
        flow.Append(new FlowDelegate(() =>
        {
            resolveContext.Invoke(Node, context);
            return Task.CompletedTask;
        }));
    }
}