using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using Nodus.NodeEditor.Views;

namespace Nodus.RenderEditor.Views;

public class RenderNode : Node
{
    protected override Port? CreatePortControl(PortViewModel vm)
    {
        return vm.Type switch
        {
            PortType.Input => new RenderInputPort { DataContext = vm },
            PortType.Output => new RenderOutputPort { DataContext = vm },
            _ => null
        };
    }
}