using System;
using System.Diagnostics;
using Avalonia.Controls;
using DynamicData;

namespace Nodus.Core.ObjectDescription;

public partial class ComboboxEditor : PropertyEditorControl
{
    public ComboboxEditor()
    {
        InitializeComponent();
        
        ValueContainer.SelectionChanged += OnSelectedValue;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (DataContext is IComboboxEditorContentProvider provider)
        {
            ValueContainer.ItemsSource = provider.GetOptions();
        }
        
        base.OnDataContextChanged(e);
    }
    
    private void OnSelectedValue(object? sender, SelectionChangedEventArgs e)
    {
        TryChangeValue(ValueContainer.SelectedValue);
    }

    protected override void OnValueChanged(object? value)
    {
        if (value == null || DataContext is not IComboboxEditorContentProvider provider) return;

        ValueContainer.SelectedIndex = provider.GetOptions().IndexOf(value.ToString());
    }
}