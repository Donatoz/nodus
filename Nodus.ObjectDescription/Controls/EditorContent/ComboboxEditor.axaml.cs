using Avalonia;
using Avalonia.Controls;
using Nodus.Core.Extensions;
using PropertyModels.Extensions;
using ReactiveUI;

namespace Nodus.ObjectDescriptor.Controls;

public partial class ComboboxEditor : ExposedValueEditorContent
{
    public Func<Type, string[]> OptionsSelector { get; set; } = SelectEnumOptions;
    public Func<string, Type, object?> ValueConverter { get; set; } = GetEnumValue;
    
    public ComboboxEditor()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            if (ViewModel != null)
            {
                Editor.Items.Clear();
                OptionsSelector.Invoke(ViewModel.ValueType).ForEach(x => Editor.Items.Add(x));
                Editor.SelectedIndex = Editor.Items.IndexOf(ViewModel.CurrentValue?.ToString() ?? string.Empty);
            }
        });
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        
        Editor.SelectionChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ViewModel == null || Editor.SelectedItem?.ToString() is not { } s) return;

        ViewModel.CurrentValue = ValueConverter.Invoke(s, ViewModel.ValueType);
    }

    protected override void OnValueExplicitlyChanged(object? value)
    {
        base.OnValueExplicitlyChanged(value);

        if (value == null)
        {
            Editor.SelectedIndex = 0;
            return;
        }

        Editor.SelectedIndex = Editor.Items.IndexOf(value.ToString());
    }

    private static string[] SelectEnumOptions(Type type)
    {
        if (!type.IsEnum) return Array.Empty<string>();

        return Enum.GetValues(type)
            .Select(x => x.ToString())
            .OfType<string>()
            .ToArray();
    }

    private static object? GetEnumValue(string option, Type valueType)
    {
        if (string.IsNullOrWhiteSpace(option)) return null;

        return Enum.TryParse(valueType, option, out var v) ? v : null;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        
        Editor.SelectionChanged -= OnSelectionChanged;
    }
}