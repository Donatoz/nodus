using Nodus.NodeEditor.Models;

namespace Nodus.RenderEditor.Models;

public interface IRenderPortModel : ITypedPortModel
{
    
}

public class RenderPortModel : TypedPortModel, IRenderPortModel
{
    public RenderPortModel(string header, PortType type, PortCapacity capacity, string? id = null) 
        : base(header, type, capacity, id)
    {
    }
}