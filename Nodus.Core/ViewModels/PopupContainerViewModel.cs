using System;
using System.Diagnostics;
using Nodus.Core.Common;
using Nodus.Core.Reactive;

namespace Nodus.Core.ViewModels;

public interface IPopupViewModel
{
    IObservable<IEvent> PopupEventStream { get; }
}

public class PopupContainerViewModel : IDisposable
{
    public IReactiveProperty<IPopupViewModel?> CurrentPopup => currentPopup;
    
    private readonly MutableReactiveProperty<IPopupViewModel?> currentPopup;
    private IDisposable? currentPopupStreamContract;

    public PopupContainerViewModel()
    {
        currentPopup = new MutableReactiveProperty<IPopupViewModel?>();
    }

    public void OpenPopup(IPopupViewModel popup)
    {
        currentPopupStreamContract?.Dispose();
        
        currentPopup.SetValue(popup);
        currentPopupStreamContract = popup.PopupEventStream.Subscribe(OnPopupEvent);
    }

    private void OnPopupEvent(IEvent evt)
    {
        if (evt is PopupCloseEvent)
        {
            ClosePopup();
        }
    }

    public void ClosePopup()
    {
        currentPopup.SetValue(null);
        currentPopupStreamContract?.Dispose();
    }

    public void Dispose()
    {
        currentPopup.Dispose();
        currentPopupStreamContract?.Dispose();
    }
}

public readonly struct PopupCloseEvent : IEvent { }