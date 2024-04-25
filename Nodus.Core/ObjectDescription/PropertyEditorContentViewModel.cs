using System;
using System.Windows.Input;
using ReactiveUI;

namespace Nodus.Core.ObjectDescription;

public class PropertyEditorContentViewModel
{
    public ICommand ChangeValue { get; }

    private readonly Action<object?> commitAction;
    private readonly Func<object?> getter;

    protected Type PropertyType { get; }

    public PropertyEditorContentViewModel(Type propertyType, Action<object?> commitAction, Func<object?> getter)
    {
        PropertyType = propertyType;
        this.commitAction = commitAction;
        this.getter = getter;

        ChangeValue = ReactiveCommand.Create<object>(OnChangeValue);
    }

    private void OnChangeValue(object? value)
    {
        commitAction.Invoke(value);
    }

    public object? GetValue() => getter.Invoke();
}