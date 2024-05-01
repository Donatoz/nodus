using System.Numerics;

namespace Nodus.NodeEditor.Meta;

public interface IGraphElementData
{
    string? ElementId { get; init; }
    
    VisualGraphElementData? VisualData { get; set; }
}

public class VisualGraphElementData
{
    public Vector2 Position { get; set; }
}