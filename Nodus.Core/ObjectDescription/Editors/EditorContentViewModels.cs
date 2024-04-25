using System;
using System.Collections.Generic;
using System.Linq;

namespace Nodus.Core.ObjectDescription;

public class StringEditorContentViewModel : PropertyEditorContentViewModel
{
    public StringEditorContentViewModel(Type propertyType, Action<string?> commitAction, Func<object?> getter) 
        : base(propertyType, s => commitAction.Invoke(s?.ToString()), getter)
    {
    }
}

public class EnumEditorContentViewModel : PropertyEditorContentViewModel, IComboboxEditorContentProvider
{
    private readonly string[] options;
    
    public EnumEditorContentViewModel(Type propertyType, Action<object?> commitAction, Func<object?> getter)
        : base(propertyType, s => commitAction.Invoke(s != null ? Enum.Parse(propertyType, s.ToString()!) : null), getter)
    {
        options = Enum.GetValues(propertyType).Cast<object>().Select(x => x.ToString()!).ToArray();
    }

    public string[] GetOptions() => options;
}