using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Nodus.Core.Controls.Templates;
using Nodus.NodeEditor.ViewModels;
using Nodus.NodeEditor.Views.Modals;

namespace Nodus.NodeEditor.Views.Templates;

[DataTemplateProvider(typeof(NodeEditorModalViewModel))]
public class ModalTemplateSelector : IDataTemplate
{
    public Control? Build(object? param) => param switch
    {
        NodeSearchModalViewModel => new NodeSearchModal(),
        _ => null
    };

    public bool Match(object? data)
    {
        return data is NodeEditorModalViewModel;
    }
}