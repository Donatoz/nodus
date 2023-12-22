using System;
using Nodus.NodeEditor.Meta;

namespace Nodus.NodeEditor.Models;

public interface IPortModel
{
    string Id { get; }
    string Header { get; }
    PortType Type { get; }
    PortCapacity Capacity { get; }

    bool IsCompatible(IPortModel other);

    PortData Serialize();
}

public class PortModel : IPortModel
{
    public string Id { get; }
    public string Header { get; }
    public PortType Type { get; }
    public PortCapacity Capacity { get; }

    public PortModel(string header, PortType type, PortCapacity capacity, string? id = null)
    {
        Id = id ?? Guid.NewGuid().ToString();
        Header = header;
        Type = type;
        Capacity = capacity;
    }

    public PortModel(PortData data) : this(data.PortHeader, data.Type, data.Capacity, data.PortId)
    {
    }

    public virtual bool IsCompatible(IPortModel other) => Type != other.Type;
    
    public PortData Serialize()
    {
        return new PortData(Header, Type, Capacity) { PortId = Id };
    }

    public override string ToString() => Id;
}

public enum PortType
{
    Input,
    Output
}

public enum PortCapacity
{
    Single,
    Multiple
}