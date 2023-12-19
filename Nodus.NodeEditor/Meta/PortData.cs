using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Meta;

public record PortData(string PortHeader, PortType Type, PortCapacity Capacity)
{
    public string? PortId { get; init; }
}