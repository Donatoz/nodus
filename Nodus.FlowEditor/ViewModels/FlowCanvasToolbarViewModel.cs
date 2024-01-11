using System;
using System.Windows.Input;
using Nodus.Core.Entities;
using Nodus.Core.ViewModels;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using ReactiveUI;

namespace FlowEditor.ViewModels;

public class FlowCanvasToolbarViewModel : NodeCanvasToolbarViewModel
{
    public ICommand OpenConsole { get; }
    
    public FlowCanvasToolbarViewModel(IServiceProvider serviceProvider, INodeCanvasModel canvasModel, INodeCanvasViewModel vm) : base(serviceProvider, canvasModel, vm)
    {
        OpenConsole = ReactiveCommand.Create(OnOpenConsole);
    }

    private void OnOpenConsole()
    {
        var log = CanvasViewModel.TryGetContainedValue<LogViewModel>();
        
        if (log == null) return;
        
        CanvasViewModel.TryGetContainedValue<PopupContainerViewModel>()?.OpenPopup(log);
    }
}