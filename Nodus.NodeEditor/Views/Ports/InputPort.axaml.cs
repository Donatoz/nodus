using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Nodus.NodeEditor.Views;

public partial class InputPort : Port
{
    public override Border PortHandler => Handler;

    public InputPort()
    {
        InitializeComponent();
    }
}