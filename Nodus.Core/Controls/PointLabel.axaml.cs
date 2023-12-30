using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Nodus.Core.Controls;

public partial class PointLabel : UserControl
{
    public static readonly StyledProperty<string> LabelProperty;
    public static readonly StyledProperty<IBrush?> DotBrushProperty;

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public IBrush? DotBrush
    {
        get => GetValue(DotBrushProperty);
        set => SetValue(DotBrushProperty, value);
    }

    static PointLabel()
    {
        LabelProperty = AvaloniaProperty.Register<PointLabel, string>(nameof(Label));
        DotBrushProperty = AvaloniaProperty.Register<PointLabel, IBrush?>(nameof(DotBrush));
    }
    
    public PointLabel()
    {
        InitializeComponent();
    }
}