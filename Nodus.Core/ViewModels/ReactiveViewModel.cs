using System;
using System.Reactive.Subjects;
using Nodus.Core.Common;
using ReactiveUI;

namespace Nodus.Core.ViewModels;

public interface IReactiveViewModel
{
    IObservable<IEvent> EventStream { get; }
}

public abstract class ReactiveViewModel : ReactiveObject, IReactiveViewModel, IDisposable
{
    private readonly BehaviorSubject<IEvent> eventSubject;

    public IObservable<IEvent> EventStream => eventSubject;

    protected ReactiveViewModel()
    {
        eventSubject = new BehaviorSubject<IEvent>(IEvent.Empty);
    }

    protected void RaiseEvent(IEvent evt)
    {
        eventSubject.OnNext(evt);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            eventSubject.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}