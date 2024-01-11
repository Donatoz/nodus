using System;
using Avalonia.Controls;

namespace Nodus.Core.ObjectDescription;

public class PropertyEditorControl : UserControl
{
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is PropertyEditorContentViewModel vm)
        {
            OnValueChanged(vm.GetValue());
        }
    }

    protected virtual void OnValueChanged(object? value)
    {
    }

    protected void TryChangeValue(object? value)
    {
        if (DataContext is PropertyEditorContentViewModel vm)
        {
            vm.ChangeValue.Execute(value);
        }
    }
}