using Nodus.NodeEditor.Views;
using Nodus.NodeEditor.Views.Templates;

namespace FlowEditor.Views.Templates;

public class FlowCanvasComponentSelector : NodeCanvasComponentsSelector
{
    protected override NodeCanvasToolbar CreateToolbar()
    {
        return new FlowCanvasToolbar();
    }
}