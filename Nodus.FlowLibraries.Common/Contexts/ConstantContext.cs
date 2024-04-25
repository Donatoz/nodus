using FlowEditor;
using FlowEditor.Models;
using FlowEditor.Models.Primitives;
using Nodus.Core.Extensions;
using Nodus.Core.Reactive;
using Nodus.NodeEditor.Meta;

namespace Nodus.FlowLibraries.Common;

public sealed class ConstantContext : FlowContextBase
{
    private object output;
    
    private MutableReactiveProperty<RuntimeType> constantType = null!;
    private IDisposable typeContract = null!;
    
    private IFlowPortModel? outPort;
    
    private readonly ValueDescriptor outputDescriptor;
    private readonly ValueDescriptor constTypeDescriptor;

    public ConstantContext()
    {
        constantType = new MutableReactiveProperty<RuntimeType>(RuntimeType.Number);

        output = 0;
        
        outputDescriptor = new ValueDescriptor(x => output = x ?? 0, () => output)
        {
            Name = "Value",
            DisplayName = "Output",
            Value = output
        };
        constTypeDescriptor = new ValueDescriptor(x => constantType.SetValue(x.MustBe<RuntimeType>()), () => constantType.Value)
        {
            Name = "Type",
            DisplayName = "Constant Type",
            Value = constantType.Value
        };

        typeContract = constantType.AlterationStream.Subscribe(OnTypeChanged);
    }
    

    private void OnTypeChanged(RuntimeType type)
    {
        outPort?.ChangeValueType(type.ToClrType());
        outputDescriptor.Value = type.GetDefaultValue();
    }

    public override void Bind(IFlowNodeModel node)
    {
        base.Bind(node);

        outPort = node.GetFlowPorts().FirstOrDefault();

        if (outPort == null) return;
        
        outPort.ChangeValueType(constantType.Value.ToClrType());
        BindPortValue(outPort, GetValue);
    }

    private object? GetValue(GraphContext _) => output;

    public override NodeContextData Serialize()
    {
        return new ConstantContextData(output, constantType.Value.ToClrType());
    }

    public override void Deserialize(NodeContextData data)
    {
        if (data is not ConstantContextData d) return;
        
        output = d.OverrideValue;
        constantType.SetValue(d.ConstantType.ToRuntimeType());
        outputDescriptor.Value = d.OverrideValue;
    }

    protected override IEnumerable<ValueDescriptor> GetDescriptors()
    {
        yield return outputDescriptor;
        yield return constTypeDescriptor;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        if (!disposing) return;
        
        constantType.Dispose();
        typeContract.Dispose();
    }
}

internal record ConstantContextData(object OverrideValue, Type ConstantType) : NodeContextData;