using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Nodus.NodeEditor.Views;

public partial class OutputPort : Port
{
    public override Border PortHandler => Handler;

    public OutputPort()
    {
        InitializeComponent();
    }
}