using Nodus.FlowEngine;
using Nodus.NodeEditor.Meta;

namespace FlowEditor.Models.Extensions;

public interface IFlowContextExtension
{
    IFlowUnit? CreateFlowUnit(GraphContext ctx);
}
