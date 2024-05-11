using Nodus.NodeEditor.Models;

namespace Nodus.RenderEditor.Models;

public interface IRenderNodeModel : INodeModel
{
    
}

public class RenderNodeModel : NodeModel, IRenderNodeModel
{
    public RenderNodeModel(string title, NodeTooltip tooltip = default, string? id = null, string? group = null, string? ctxId = null) 
        : base(title, tooltip, id, group, ctxId)
    {
    }
}