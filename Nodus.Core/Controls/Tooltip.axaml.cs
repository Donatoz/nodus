using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Nodus.Core.Extensions;

namespace Nodus.Core.Controls;

public partial class Tooltip : UserControl
{
    public static readonly StyledProperty<string> HeaderProperty;
    public static readonly StyledProperty<string> TextProperty;
    public static readonly StyledProperty<Control?> ContainerProperty;

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
    
    public Control? Container
    {
        get => GetValue(ContainerProperty);
        set => SetValue(ContainerProperty, value);
    }

    private TranslateTransform? translate;

    static Tooltip()
    {
        HeaderProperty = AvaloniaProperty.Register<Tooltip, string>(nameof(Header));
        TextProperty = AvaloniaProperty.Register<Tooltip, string>(nameof(Text));
        ContainerProperty = AvaloniaProperty.Register<Tooltip, Control?>(nameof(Container));
    }
    
    public Tooltip()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        var borderAncestor = this.FindAncestorOfType<Border>();
        
        if (borderAncestor != null)
        {
            if (borderAncestor.RenderTransform is not TranslateTransform)
            {
                borderAncestor.RenderTransform = new TranslateTransform();
            }

            borderAncestor.ClipToBounds = false;
            translate = (borderAncestor.RenderTransform as TranslateTransform)!;
        }
        
        Container?.AddHandler(PointerMovedEvent, OnPointerMoved);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        return;
        
        if (translate == null) return;
        
        var pos = e.GetCurrentPoint(Container);
        translate.X = pos.Position.X;
        translate.Y = pos.Position.Y;
    }
}