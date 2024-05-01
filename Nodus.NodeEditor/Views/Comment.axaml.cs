using Avalonia.Controls;

namespace Nodus.NodeEditor.Views;

public partial class Comment : GraphElement
{
    protected override Control ContainerControl => Container;
    protected override Control BodyControl => Body;
    
    public Comment()
    {
        InitializeComponent();
    }
}