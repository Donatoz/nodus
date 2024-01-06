using System;
using Nodus.Core.Extensions;
using Nodus.Core.ObjectDescription;
using Nodus.NodeEditor.Models;

namespace FlowEditor.Models.Contexts;

public class ConstantContext : FlowNodeContextBase
{
    [ExposedProperty("This is multiplier for this property editor. Use wisely.")]
    public string Multiplier { get; set; }
    
    private readonly Func<IFlowPortModel, Func<object>> outputPortBindingFactory;
    
    public ConstantContext(Func<IFlowPortModel, Func<object>> outputPortBindingFactory)
    {
        this.outputPortBindingFactory = outputPortBindingFactory;
    }

    public override void Bind(IFlowNodeModel node)
    {
        base.Bind(node);

        node.GetFlowPorts().ForEach(x => BindPortValue(x, outputPortBindingFactory.Invoke(x)));
    }
}