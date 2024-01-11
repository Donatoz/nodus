using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Avalonia;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Meta;

public class NodeData
{
    public string? NodeId { get; init; }
    public string? Group { get; init; }
    public NodeVisualData? VisualData { get; set; }
    public string Title { get; init; }
    public NodeTooltip Tooltip { get; init; }
    public PortData[] Ports { get; init; }
    public string? ContextId { get; init; }
    public NodeContextData? ContextData { get; init; }
    
    public NodeData(string title, NodeTooltip tooltip, IEnumerable<PortData> ports)
    {
        Title = title;
        Tooltip = tooltip;
        Ports = ports.ToArray();
    }
}

public class NodeVisualData
{
    public Point Position { get; set; }
}