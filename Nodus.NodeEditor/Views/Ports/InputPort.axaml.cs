using Avalonia.Layout;

namespace Nodus.NodeEditor.Views;

public partial class InputPort : Port
{
    public override Layoutable PortHandler => Handler;

    public InputPort()
    {
        InitializeComponent();
    }
}