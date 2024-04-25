using System.Diagnostics;
using FlowEditor;
using FlowEditor.Meta;
using FlowEditor.Models;
using FlowEditor.Models.Contexts;
using Nodus.Core.Extensions;
using Nodus.FlowEngine;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace Nodus.FlowLibraries.Common;

public class BranchContext : CachedExposeContext
{
    private IFlowPortModel? inPort;
    private IFlowPortModel? trueOutPort;
    private IFlowPortModel? falseOutPort;

    private const string DefaultValueName = "DefaultValue";
    
    public BranchContext()
    {
        ExposeValue(DefaultValueName, "Default Value", false);
    }

    public override void Bind(IFlowNodeModel node)
    {
        base.Bind(node);

        inPort = node.GetFlowPorts().FirstOrDefault(x => x.Type == PortType.Input && x.ValueType.Value == typeof(bool));
        
        var outPorts = node.GetFlowPorts().Where(x => x.Type == PortType.Output && x.ValueType.Value == typeof(FlowType));

        if (outPorts.Count() < 2)
        {
            throw new Exception("A branch context must have at least 2 unique output flow ports");
        }

        trueOutPort = outPorts.First();
        falseOutPort = outPorts.Last();
    }

    protected override void AlterFlow(IFlow flow, GraphContext context, IFlowToken currentToken)
    {
        var direction = Node?.GetPortValue(inPort.NotNull().Id, context) is bool b
            ? b
            : GetExposedValue<bool>(DefaultValueName);

        currentToken.Successor = direction
            ? currentToken.DescendantTokens?.FirstOrDefault()
            : currentToken.DescendantTokens?.LastOrDefault();
    }

    public override IFlowPortModel? GetEffectiveSuccessionPort(GraphContext ctx)
    {
        return Node?.GetPortValue(inPort.NotNull().Id, ctx) is bool b
            ? b
                ? trueOutPort
                : falseOutPort
            : GetExposedValue<bool>(DefaultValueName)
                ? trueOutPort
                : falseOutPort;
    }
}