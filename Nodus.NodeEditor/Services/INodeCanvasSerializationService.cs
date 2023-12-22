using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;

namespace Nodus.NodeEditor.Services;

public interface INodeCanvasSerializationService
{
    void PopulateCanvas(INodeCanvasModel canvas);
    void SaveGraph(INodeCanvasModel canvas);
}