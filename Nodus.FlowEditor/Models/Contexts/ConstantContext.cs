using System;
using System.Diagnostics;
using Nodus.Core.Extensions;
using Nodus.Core.Reactive;
using Nodus.NodeEditor.Meta;

namespace FlowEditor.Models.Contexts;

public class ConstantContext : FlowContextBase
{
    private readonly MutableReactiveProperty<string> overrideOutput;
    private readonly Func<IFlowPortModel, ConstantContext, Func<object?>> outputPortBindingFactory;
    
    public ConstantContext(Func<IFlowPortModel, ConstantContext, Func<object?>> outputPortBindingFactory)
    {
        this.outputPortBindingFactory = outputPortBindingFactory;
        overrideOutput = new MutableReactiveProperty<string>();
    }

    public override void Bind(IFlowNodeModel node)
    {
        base.Bind(node);

        node.GetFlowPorts().ForEach(x => BindPortValue(x, outputPortBindingFactory.Invoke(x, this)));
    }

    protected override IFlowContextMutator CreateMutator()
    {
        return new GenericFlowContextMutator(
            new FlowContextMutatorProperty("Override Output", typeof(string), 
                new ValueBinding(() => overrideOutput.Value, v => overrideOutput.SetValue(v?.MustBeOfType<string>() ?? string.Empty)),
                "Overrides output for the Value port")
        );
    }

    public override NodeContextData Serialize()
    {
        return new ConstantContextData(overrideOutput.Value);
    }

    public override void Deserialize(NodeContextData data)
    {
        if (data is not ConstantContextData d) return;
        
        overrideOutput.SetValue(d.OverrideValue);
    }
}

internal record ConstantContextData(string OverrideValue) : NodeContextData;