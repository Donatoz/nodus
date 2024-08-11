using System.Diagnostics;
using Avalonia;
using Avalonia.Interactivity;

namespace Nodus.ObjectDescriptor.Controls;

public partial class CheckboxEditor : ExposedValueEditorContent
{
    public CheckboxEditor()
    {
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        Editor.IsCheckedChanged += OnCheckedChanged;
    }

    private void OnCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (ViewModel == null) return;

        ViewModel.CurrentValue = Editor.IsChecked;
    }

    protected override void OnValueExplicitlyChanged(object? value)
    {
        base.OnValueExplicitlyChanged(value);

        if (value == null)
        {
            Editor.IsChecked = false;
            return;
        }
        
        if (value is not bool b) return;

        Editor.IsChecked = b;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        Editor.IsCheckedChanged -= OnCheckedChanged;
    }
}