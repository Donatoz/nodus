using Avalonia.Interactivity;

namespace Nodus.NodeEditor.Views;

public partial class InputPort : Port
{
    public override Interactive PortHandler => Handler;

    public InputPort()
    {
        InitializeComponent();
    }
}