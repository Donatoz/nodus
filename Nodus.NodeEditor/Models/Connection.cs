using Nodus.NodeEditor.Meta;

namespace Nodus.NodeEditor.Models;

public readonly struct Connection
{
    public string SourceNodeId { get; }
    public string TargetNodeId { get; }
    public string SourcePortId { get; }
    public string TargetPortId { get; }

    public Connection(string sourceNodeId, string targetNodeId, string sourcePortId, string targetPortId)
    {
        SourceNodeId = sourceNodeId;
        TargetNodeId = targetNodeId;
        SourcePortId = sourcePortId;
        TargetPortId = targetPortId;
    }
}