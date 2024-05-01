using System.Collections.Generic;
using System.Linq;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Meta;

public class NodeData : IGraphElementData
{
    public string? ElementId { get; init; }
    public string? Group { get; init; }
    public string Title { get; init; }
    public NodeTooltip Tooltip { get; init; }
    public PortData[] Ports { get; init; }
    public string? ContextId { get; init; }
    public NodeContextData? ContextData { get; init; }
    public VisualGraphElementData? VisualData { get; set; }
    
    public NodeData(string title, NodeTooltip tooltip, IEnumerable<PortData> ports)
    {
        Title = title;
        Tooltip = tooltip;
        Ports = ports.ToArray();
    }
}