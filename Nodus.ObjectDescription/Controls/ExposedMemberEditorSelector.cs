using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Nodus.ObjectDescriptor.ViewModels;

namespace Nodus.ObjectDescriptor.Controls;

public class ExposedMemberEditorSelector : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is not ExposedMemberViewModel vm) return null;

        return vm switch
        {
            ExposedCompoundViewModel => new ExposedCompoundEditor {DataContext = vm},
            ExposedCollectionViewModel => new ExposedCollectionEditor {DataContext = vm},
            ExposedMethodViewModel => new ExposedMethodEditor {DataContext = vm},
            _ => new ExposedValueEditor {DataContext = vm}
        };
    }

    public bool Match(object? data)
    {
        return data is ExposedMemberViewModel;
    }
}