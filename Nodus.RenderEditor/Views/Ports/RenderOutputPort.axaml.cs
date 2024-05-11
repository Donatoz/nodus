using Avalonia.Controls;
using Nodus.NodeEditor.Views;

namespace Nodus.RenderEditor.Views;

public partial class RenderOutputPort : TypedPort
{
    public override Border PortHandler => Handler;
    protected override Control? TooltipContainer => Container;

    public RenderOutputPort()
    {
        InitializeComponent();
    }
}