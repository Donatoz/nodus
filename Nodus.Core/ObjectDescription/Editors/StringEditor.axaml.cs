using Avalonia.Controls;

namespace Nodus.Core.ObjectDescription;

public partial class StringEditor : PropertyEditorControl
{
    public StringEditor()
    {
        InitializeComponent();
        
        Field.AddHandler(TextBox.TextChangedEvent, OnTextChanged);
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        TryChangeValue(Field.Text);
    }

    protected override void OnValueChanged(object? value)
    {
        Field.Text = value?.ToString() ?? string.Empty;
    }
}