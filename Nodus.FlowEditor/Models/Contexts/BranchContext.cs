using Nodus.FlowEngine;
using Nodus.NodeEditor.Meta;

namespace FlowEditor.Models.Contexts;

public class BranchContext : FlowContextBase
{
    protected override void Resolve(IFlow flow, GraphContext context, IFlowToken currentToken)
    {
        base.Resolve(flow, context, currentToken);
    }
}