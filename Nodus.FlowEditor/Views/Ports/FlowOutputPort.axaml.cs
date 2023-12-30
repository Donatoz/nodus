using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace FlowEditor.Views;

public partial class FlowOutputPort : FlowPort
{
    public override Border PortHandler => Handler;
    protected override Control PortContainer => Container;

    public FlowOutputPort()
    {
        InitializeComponent();
    }
}