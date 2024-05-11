using FlowEditor;
using FlowEditor.Models;
using FlowEditor.Models.Primitives;
using Nodus.Core.Extensions;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace Nodus.FlowLibraries.Common;

public sealed class ArithmeticsContext : FlowContextBase
{
    private ArithmeticsOperation operation;

    private readonly ValueDescriptor operationDescriptor;

    public ArithmeticsContext()
    {
        operationDescriptor = new ValueDescriptor(x => operation = x.MustBe<ArithmeticsOperation>(), () => operation)
        {
            Name = nameof(operation),
            DisplayName = "Operation",
            Value = operation
        };
    }

    public override void Bind(INodeModel node)
    {
        base.Bind(node);

        TryBindFirstOutPort(GetValue);
    }
 
    private object GetValue(GraphContext context)
    {
        var inputPorts = Node.GetFlowPorts().Where(x => x.Type == PortType.Input 
                                                         && x.ValueType.Value == typeof(float));
        
        if (!inputPorts.Any()) return 0f;

        var result = inputPorts
            .Select(x => Node.GetPortValue(x.Id, context) is float f ? f : 0f)
            .Aggregate((a, b) => operation.Resolve(a, b));

        return result;
    }

    protected override IEnumerable<ValueDescriptor> GetDescriptors()
    {
        yield return operationDescriptor;
    }

    public override NodeContextData Serialize()
    {
        return new ArithmeticsContextData(operation);
    }

    public override void Deserialize(NodeContextData data)
    {
        if (data is not ArithmeticsContextData d) return;

        operation = d.Operation;
    }
}

internal record ArithmeticsContextData(ArithmeticsOperation Operation) : NodeContextData;