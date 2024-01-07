using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;

namespace Nodus.App.Views;

public partial class TestImg : UserControl
{
    public static StyledProperty<Control?> TargetProperty;
    public static StyledProperty<Bitmap?> BitmapProperty;

    public Control? Target
    {
        get => GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    public Bitmap? Bitmap
    {
        get => GetValue(BitmapProperty);
        set => SetValue(BitmapProperty, value);
    }

    static TestImg()
    {
        TargetProperty = AvaloniaProperty.Register<TestImg, Control?>(nameof(Target));
        BitmapProperty = AvaloniaProperty.Register<TestImg, Bitmap?>(nameof(Bitmap));
    }
    
    public TestImg()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (Target != null)
        {
            var s = new Size(Target.Width, Target.Height);
            var t = new RenderTargetBitmap(new PixelSize((int)Target.Width + 1, (int)Target.Height + 1), new Vector(96, 96));
            
            Target.Measure(s);
            Target.Arrange(new Rect(s));
            
            t.Render(Target);

            Bitmap = t;
        }
    }
}