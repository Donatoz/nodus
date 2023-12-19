using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Nodus.Core.Controls;

public partial class Tooltip : UserControl
{
    public static readonly StyledProperty<string> HeaderProperty = AvaloniaProperty.Register<Tooltip, string>(nameof(Header));
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<Tooltip, string>(nameof(Text));

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }
    
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    
    public Tooltip()
    {
        InitializeComponent();
    }
}