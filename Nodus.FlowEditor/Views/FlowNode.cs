using Avalonia.Controls;
using FlowEditor.ViewModels;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using Nodus.NodeEditor.Views;
using ReactiveUI;

namespace FlowEditor.Views;

public class FlowNode : Node
{
    protected override void OnInitialized()
    {
        base.OnInitialized();

        Menu.Items.Add(new MenuItem
        {
            Header = "Run Flow",
            Command = ReactiveCommand.Create(OnRunFlow)
        });
    }

    private void OnRunFlow()
    {
        if (DataContext is FlowNodeViewModel vm)
        {
            vm.RunFlow();
        }
    }

    protected override Port? CreatePortControl(PortViewModel vm)
    {
        return vm.Type switch
        {
            PortType.Input => new FlowInputPort { DataContext = vm },
            PortType.Output => new FlowOutputPort { DataContext = vm },
            _ => null
        };
    }
}