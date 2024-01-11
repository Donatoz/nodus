using System;
using Nodus.Core.Reactive;

namespace Nodus.Core.ViewModels;

public class PopupContainerViewModel : IDisposable
{
    private readonly MutableReactiveProperty<object?> currentPopup;

    public IReactiveProperty<object?> CurrentPopup => currentPopup;

    public PopupContainerViewModel()
    {
        currentPopup = new MutableReactiveProperty<object?>();
    }

    public void OpenPopup(object popup)
    {
        currentPopup.SetValue(popup);
    }

    public void ClosePopup()
    {
        currentPopup.SetValue(null);
    }

    public void Dispose()
    {
        currentPopup.Dispose();
    }
}