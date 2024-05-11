using FlowEditor.Models;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;

namespace FlowEditor.ViewModels;

public class FlowPortViewModel : TypedPortViewModel
{
    public FlowPortViewModel(IFlowPortModel model) : base(model)
    {
    }
}