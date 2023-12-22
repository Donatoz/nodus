using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
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

    public virtual void UpdatePath(Point from, Point to, IList<LineSegment> lines)
    {
        const ushort lineFixedSpan = 30;
        var direction = (Point) to - (Point) from;

        lines[0].Point = new Point(lineFixedSpan, 0);
        lines[1].Point = direction - new Point(lineFixedSpan, 0);
        lines[2].Point = direction;
    }
}