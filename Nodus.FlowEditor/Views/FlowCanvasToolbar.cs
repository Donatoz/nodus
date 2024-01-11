using Avalonia.Controls;
using FlowEditor.ViewModels;
using Nodus.NodeEditor.Views;
using Projektanker.Icons.Avalonia;
using ReactiveUI;

namespace FlowEditor.Views;

public class FlowCanvasToolbar : NodeCanvasToolbar
{
    protected override void OnInitialized()
    {
        base.OnInitialized();

        var consoleButton = new Button { FontSize = 18, Command = ReactiveCommand.Create(OnOpenConsole)};
        Attached.SetIcon(consoleButton, "fa-solid fa-terminal");
        
        ExtensionsPanel.Children.Add(consoleButton);
    }

    private void OnOpenConsole()
    {
        if (DataContext is not FlowCanvasToolbarViewModel vm) return;

        vm.OpenConsole.Execute(null);
    }
}