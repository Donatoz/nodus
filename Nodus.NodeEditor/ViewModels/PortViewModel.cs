using Nodus.NodeEditor.Models;
using ReactiveUI;

namespace Nodus.NodeEditor.ViewModels;

public class PortViewModel : ReactiveObject
{
    public string PortId { get; }
    public string Label { get; }
    public PortType Type { get; }
    
    public PortViewModel(IPortModel model)
    {
        PortId = model.Id;
        Label = model.Header;
        Type = model.Type;
    }
}