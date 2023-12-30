using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using DynamicData;
using Nodus.NodeEditor.ViewModels;

namespace Nodus.NodeEditor.Views;

public partial class ConnectionPath : UserControl
{
    private readonly LinearGradientBrush pathBrush;
    
    public ConnectionPath()
    {
        pathBrush = new LinearGradientBrush();
        
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        PathContainer.Stroke = pathBrush;
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
        var direction = to - from;

        lines[0].Point = new Point(lineFixedSpan, 0);
        lines[1].Point = direction - new Point(lineFixedSpan, 0);
        lines[2].Point = direction;
        
        pathBrush.StartPoint = new RelativePoint(default, RelativeUnit.Absolute);
        pathBrush.EndPoint = new RelativePoint(direction, RelativeUnit.Absolute);
    }

    public virtual void UpdateColor(Color from, Color to)
    {
        pathBrush.GradientStops = new GradientStops
        {
            new(from, 0),
            new(to, 1)
        };
    }
}