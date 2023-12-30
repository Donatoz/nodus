using System;
using Newtonsoft.Json;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace FlowEditor.Meta;

public record FlowPortData : PortData
{
    public Type ValueType { get; init; }

    [JsonConstructor]
    public FlowPortData(string portHeader, PortType type, PortCapacity capacity, Type valueType) : base(portHeader, type, capacity)
    {
        ValueType = valueType;
    }
    
    public FlowPortData(PortData parent, Type valueType) : this(parent.PortHeader, parent.Type, parent.Capacity, valueType)
    {
        PortId = parent.PortId;
    }
}