using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Nodus.ObjectDescriptor.ViewModels;

namespace Nodus.ObjectDescriptor.Controls;

public class ExposedValueEditorContentSelector : IDataTemplate
{
    private static readonly IDictionary<Type, Func<ExposedValueViewModel, Control>> Factories =
        new Dictionary<Type, Func<ExposedValueViewModel, Control>>
        {
            {typeof(float), CommonTextboxEditors.FloatEditor},
            {typeof(int), CommonTextboxEditors.IntEditor},
            {typeof(string), CommonTextboxEditors.StringEditor},
            {typeof(bool), d => new CheckboxEditor {DataContext = d}},
            {typeof(Enum), d => new ComboboxEditor {DataContext = d}}
        };
    
    public Control? Build(object? param)
    {
        if (param is not ExposedValueViewModel vm) return null;
        
        return Factories.FirstOrDefault(x => x.Key.IsAssignableFrom(vm.ValueType)).Value?.Invoke(vm);
    }

    public bool Match(object? data)
    {
        return data is ExposedValueViewModel;
    }
}