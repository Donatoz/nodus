using System;
using System.Diagnostics;
using System.Reactive;
using Nodus.Core.Common;

namespace Nodus.Core.Extensions;

public static class ObservableExtensions
{
    public static IDisposable OnEvent<T>(this IObservable<IEvent> o, Action<T> handler) where T : IEvent
    {
        return o.Subscribe(Observer.Create<IEvent>(evt =>
        {
            if (evt.GetType() == typeof(T))
            {
                handler.Invoke((T) evt);
            }
        }));
    }
}