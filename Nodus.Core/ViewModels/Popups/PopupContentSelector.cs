using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Nodus.Core.Controls;
using Nodus.Core.Controls.Templates;

namespace Nodus.Core.ViewModels;

[DataTemplateProvider(typeof(IPopupViewModel))]
public class PopupContentSelector : IDataTemplate
{
    public Control? Build(object? param) => param switch
    {
        LogViewModel => new Log(),
        _ => null
    };
    
    public bool Match(object? data)
    {
        return data is LogViewModel;
    }
}