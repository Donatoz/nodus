using Avalonia.Layout;

namespace Nodus.NodeEditor.Views;

public partial class OutputPort : Port
{
    public override Layoutable PortHandler => Handler;

    public OutputPort()
    {
        InitializeComponent();
    }
}