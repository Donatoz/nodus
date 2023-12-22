using Avalonia.Interactivity;

namespace Nodus.NodeEditor.Views;

public partial class OutputPort : Port
{
    public override Interactive PortHandler => Handler;

    public OutputPort()
    {
        InitializeComponent();
    }
}