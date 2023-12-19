using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Nodus.NodeEditor.ViewModels;

namespace Nodus.NodeEditor.Views;

public partial class ConnectionPath : UserControl
{
    public Path Path => PathContainer;
    
    public ConnectionPath()
    {
        InitializeComponent();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (e.GetCurrentPoint(PathContainer).Properties.IsLeftButtonPressed && e.ClickCount == 2 && DataContext is ConnectionViewModel vm)
        {
            vm.DeleteSelf.Execute(null);
        }
    }
}