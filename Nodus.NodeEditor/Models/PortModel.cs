using System;

namespace Nodus.NodeEditor.Models;

public interface IPortModel
{
    string Id { get; }
    string Header { get; }
    PortType Type { get; }
    PortCapacity Capacity { get; }

    bool IsCompatible(IPortModel other);
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

    public virtual bool IsCompatible(IPortModel other) => Type != other.Type;

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