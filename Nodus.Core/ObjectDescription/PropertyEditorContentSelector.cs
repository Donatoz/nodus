using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Nodus.Core.Controls.Templates;

namespace Nodus.Core.ObjectDescription;

[DataTemplateProvider(typeof(PropertyEditorContentViewModel))]
public class PropertyEditorContentSelector : IDataTemplate
{
    public Control? Build(object? param) => param switch
    {
        StringEditorContentViewModel => new StringEditor(),
        EnumEditorContentViewModel => new ComboboxEditor(),
        _ => null
    };

    public bool Match(object? data)
    {
        return data is PropertyEditorContentViewModel;
    }
}