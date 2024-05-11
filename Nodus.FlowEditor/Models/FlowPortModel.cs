using FlowEditor.Meta;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace FlowEditor.Models;

public interface IFlowPortModel : ITypedPortModel
{
}

public class FlowPortModel : TypedPortModel, IFlowPortModel
{
    public FlowPortModel(string header, PortType type, PortCapacity capacity, string? id = null) : base(header, type, capacity, id)
    {
    }

    public override PortData Serialize()
    {
        return new FlowPortData(base.Serialize(), ValueType.Value);
    }
}