namespace Nodus.NodeEditor.Meta;

public class CommentData : IGraphElementData
{
    public string? ElementId { get; init; }
    public string Content { get; }
    public VisualGraphElementData? VisualData { get; set; }

    public CommentData(string content)
    {
        Content = content;
    }
}