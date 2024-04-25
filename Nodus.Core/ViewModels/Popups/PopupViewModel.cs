using System;
using System.Windows.Input;
using Nodus.Core.Common;
using ReactiveUI;

namespace Nodus.Core.ViewModels;

public class PopupViewModel : ReactiveViewModel, IPopupViewModel
{
    public IObservable<IEvent> PopupEventStream => EventStream;
    public ICommand ClosePopupCommand { get; }

    public PopupViewModel()
    {
        ClosePopupCommand = ReactiveCommand.Create(OnClosePopup);
    }

    private void OnClosePopup()
    {
        RaiseEvent(new PopupCloseEvent());
    }
}