using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Nodus.NodeEditor.Views;

namespace FlowEditor.Views;

public partial class FlowInputPort : FlowPort
{
    public override Border PortHandler => Handler;
    protected override Control? TooltipContainer => Container;

    public FlowInputPort()
    {
        InitializeComponent();
    }
}