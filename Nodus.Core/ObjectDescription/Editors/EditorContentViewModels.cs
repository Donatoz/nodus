using System;

namespace Nodus.Core.ObjectDescription;

public class StringEditorContentViewModel : PropertyEditorContentViewModel
{
    public StringEditorContentViewModel(Action<string?> commitAction) : base(s => commitAction.Invoke(s?.ToString()))
    {
    }
}