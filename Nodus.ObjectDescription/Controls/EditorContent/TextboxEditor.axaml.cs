using Avalonia;
using Avalonia.Controls;
using Nodus.ObjectDescriptor.ViewModels;

namespace Nodus.ObjectDescriptor.Controls;

public partial class TextboxEditor : ExposedValueEditorContent
{
    public Func<string, object?> TextValueConverter { get; set; } = x => x;
    public Func<char, bool> InputValidator { get; set; } = _ => true;
    public string DefaultText { get; set; } = string.Empty;
    
    public TextboxEditor()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        Editor.TextChanging += OnTextChanging;
        Editor.TextChanged += OnTextChanged;
    }

    private void OnTextChanging(object? sender, TextChangingEventArgs e)
    {
        if (!Editor.Text?.All(InputValidator) ?? false)
        {
            Editor.Text = string.Concat(Editor.Text.Where(InputValidator));
        }
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (ViewModel == null) return;

        if (string.IsNullOrWhiteSpace(Editor.Text))
        {
            Editor.Text = DefaultText;
            return;
        }

        var convertedValue =
            TextValueConverter.Invoke(string.IsNullOrWhiteSpace(Editor.Text) ? DefaultText : Editor.Text);

        if (convertedValue != null)
        {
            ViewModel.CurrentValue = convertedValue;
        }
    }

    protected override void OnValueExplicitlyChanged(object? value)
    {
        base.OnValueExplicitlyChanged(value);
        
        Editor.Text = value?.ToString() ?? DefaultText;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        Editor.TextChanging -= OnTextChanging;
        Editor.TextChanged -= OnTextChanged;
    }
}

public static class CommonTextboxEditors
{
    public static Func<ExposedValueViewModel, TextboxEditor> StringEditor => d => new TextboxEditor
    {
        DataContext = d
    };
    
    public static Func<ExposedValueViewModel, TextboxEditor> FloatEditor => d => new TextboxEditor
    {
        DataContext = d,
        TextValueConverter = x => float.TryParse(x, out var f) ? f : null,
        InputValidator = x => char.IsDigit(x) || x == '.' || x == '-',
        DefaultText = "0"
    };
    
    public static Func<ExposedValueViewModel, TextboxEditor> IntEditor => d => new TextboxEditor
    {
        DataContext = d,
        TextValueConverter = x => int.TryParse(x, out var i) ? i : null,
        InputValidator = x => char.IsDigit(x) || x == '-',
        DefaultText = "0"
    };
}