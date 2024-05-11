using System;
using Newtonsoft.Json;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace FlowEditor.Meta;

public record FlowPortData : TypedPortData
{
    [JsonConstructor]
    public FlowPortData(string portHeader, PortType type, PortCapacity capacity, Type valueType) : base(portHeader, type, capacity, valueType)
    {
    }

    public FlowPortData(PortData parent, Type valueType) : base(parent, valueType)
    {
    }
}