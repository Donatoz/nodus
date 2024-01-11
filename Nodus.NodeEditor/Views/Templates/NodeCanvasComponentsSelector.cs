using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Nodus.NodeEditor.ViewModels;

namespace Nodus.NodeEditor.Views.Templates;

public class NodeCanvasComponentsSelector : IDataTemplate
{
    public Control? Build(object? param) => param switch
    {
        NodeCanvasToolbarViewModel => CreateToolbar(),
        NodeContextContainerViewModel => CreateNodeContextContainer(),
        _ => null
    };

    public bool Match(object? data)
    {
        return data is NodeCanvasToolbarViewModel or NodeContextContainerViewModel;
    }

    protected virtual NodeCanvasToolbar CreateToolbar()
    {
        return new NodeCanvasToolbar();
    }

    protected virtual NodeContextContainer CreateNodeContextContainer()
    {
        return new NodeContextContainer();
    }
}