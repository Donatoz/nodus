using ReactiveUI;

namespace Nodus.NodeEditor.ViewModels;

public abstract class NodeEditorModalViewModel : ReactiveObject
{
    protected INodeCanvasOperatorViewModel CanvasOperator { get; }
    
    public NodeEditorModalViewModel(INodeCanvasOperatorViewModel canvasOperator)
    {
        CanvasOperator = canvasOperator;
    }
}