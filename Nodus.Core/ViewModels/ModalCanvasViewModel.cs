using System;
using Nodus.Core.Common;
using Nodus.Core.Reactive;
using ReactiveUI;

namespace Nodus.Core.ViewModels;

public class ModalCanvasViewModel : ReactiveViewModel
{
    private MutableReactiveProperty<ReactiveObject?> currentModal;
    
    public IReactiveProperty<ReactiveObject?> CurrentModal => currentModal;
    public bool IsModalOpened => CurrentModal.Value != null;

    public ModalCanvasViewModel()
    {
        currentModal = new MutableReactiveProperty<ReactiveObject?>();
    }

    public void OpenModal(ReactiveObject modal)
    {
        currentModal.SetValue(modal);
        RaiseEvent(new ModalStateEvent(true));
    }

    public void CloseModal()
    {
        currentModal.SetValue(null);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            currentModal.Dispose();
        }
    }
}

public readonly struct ModalStateEvent : IEvent
{
    public bool IsOpen { get; }

    public ModalStateEvent(bool isOpen)
    {
        IsOpen = isOpen;
    }
}