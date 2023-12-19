using System.Collections.Generic;
using System.Linq;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Meta;

public record NodeData(string Title, NodeTooltip Tooltip, IEnumerable<PortData> Ports)
{
    public string? NodeId { get; init; }
    public string? Group { get; init; }

    public PortData? FindPort(string portId)
    {
        return Ports.FirstOrDefault(x => x.PortId == portId);
    }
}