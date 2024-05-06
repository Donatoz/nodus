using System;
using System.Threading;
using System.Threading.Tasks;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Meta;

namespace FlowEditor.Models.Contexts;

public sealed class GenericFlowContext : FlowContextBase
{
    public delegate Task GenericFlowHandler(IFlowNodeModel node, GraphContext graph, CancellationToken ct);
    
    private readonly GenericFlowHandler resolveContext;
    
    public GenericFlowContext(GenericFlowHandler resolveContext)
    {
        this.resolveContext = resolveContext;
    }

    protected override Task Resolve(GraphContext context, IFlowToken currentToken, CancellationToken ct)
    {
        if (Node == null)
        {
            throw new Exception("Failed to resolve node context: node is not bound.");
        }

        return resolveContext.Invoke(Node, context, ct);
    }
}