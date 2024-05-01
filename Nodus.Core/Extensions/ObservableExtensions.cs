using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
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

    public static void RequestMutation<T>(this ISubject<IEvent> subject, T item) where T : class
    {
        subject.OnNext(new MutationEvent<T>(item));
    }

    public static IObservable<IChangeSet<T>> TunnelAdditions<T>(this IObservable<IChangeSet<T>> o, Action<T> onAddition)
    {
        return o.Do(changes =>
        {
            foreach (var change in changes)
            {
                if (change.Reason == ListChangeReason.Add)
                {
                    onAddition.Invoke(change.Item.Current);
                }
                else if (change.Reason == ListChangeReason.AddRange)
                {
                    change.Range.ForEach(onAddition.Invoke);
                }
            }
        });
    }
    
    public static IObservable<IChangeSet<T>> TunnelRemovals<T>(this IObservable<IChangeSet<T>> o, Action<T> onRemoval)
    {
        return o.Do(changes =>
        {
            foreach (var change in changes)
            {
                if (change.Reason == ListChangeReason.Remove)
                {
                    onRemoval.Invoke(change.Item.Current);
                }
                else if (change.Reason is ListChangeReason.RemoveRange or ListChangeReason.Clear)
                {
                    change.Range.ForEach(onRemoval.Invoke);
                }
            }
        });
    }

    public static IObservable<IChangeSet<T>> TunnelChanges<T>(this IObservable<IChangeSet<T>> o, Action<T> onAddition,
        Action<T> onRemoval)
    {
        return o.TunnelAdditions(onAddition).TunnelRemovals(onRemoval);
    }
}