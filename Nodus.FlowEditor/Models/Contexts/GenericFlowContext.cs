using System;
using System.Threading.Tasks;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Meta;

namespace FlowEditor.Models.Contexts;

public sealed class GenericFlowContext : FlowNodeContextBase
{
    public delegate Task GenericFlowHandler(IFlowNodeModel node, GraphContext graph);
    
    private readonly GenericFlowHandler resolveContext;
    
    public GenericFlowContext(GenericFlowHandler resolveContext)
    {
        this.resolveContext = resolveContext;
    }

    protected override void Resolve(IFlow flow, GraphContext context)
    {
        base.Resolve(flow, context);
        
        if (Node == null)
        {
            throw new Exception("Failed to resolve node context: node is not bound.");
        }
        
        flow.Append(new FlowDelegate(() => resolveContext.Invoke(Node, context)));
    }
}