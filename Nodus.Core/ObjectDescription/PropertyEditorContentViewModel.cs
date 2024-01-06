using System;
using System.Windows.Input;
using ReactiveUI;

namespace Nodus.Core.ObjectDescription;

public class PropertyEditorContentViewModel
{
    public ICommand ChangeValue { get; }

    private readonly Action<object?> commitAction;

    public PropertyEditorContentViewModel(Action<object?> commitAction)
    {
        this.commitAction = commitAction;
        
        ChangeValue = ReactiveCommand.Create<object>(OnChangeValue);
    }

    private void OnChangeValue(object? value)
    {
        commitAction.Invoke(value);
    }
}