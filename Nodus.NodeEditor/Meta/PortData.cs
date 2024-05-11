using System;
using Newtonsoft.Json;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Meta;

public record PortData(string PortHeader, PortType Type, PortCapacity Capacity)
{
    public string? PortId { get; init; }
}

public record TypedPortData : PortData
{
    public Type ValueType { get; init; }

    [JsonConstructor]
    public TypedPortData(string portHeader, PortType type, PortCapacity capacity, Type valueType) : base(portHeader, type, capacity)
    {
        ValueType = valueType;
    }
    
    public TypedPortData(PortData parent, Type valueType) : this(parent.PortHeader, parent.Type, parent.Capacity, valueType)
    {
        PortId = parent.PortId;
    }
}