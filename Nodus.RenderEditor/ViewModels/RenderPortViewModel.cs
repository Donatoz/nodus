using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using Nodus.RenderEditor.Models;

namespace Nodus.RenderEditor.ViewModels;

public class RenderPortViewModel : TypedPortViewModel
{
    public RenderPortViewModel(IRenderPortModel model) : base(model)
    {
    }
}