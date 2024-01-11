using System;
using System.Reactive.Subjects;
using Nodus.Core.Common;
using Nodus.Core.Entities;
using ReactiveUI;

namespace Nodus.Core.ViewModels;

public interface IReactiveViewModel
{
    IObservable<IEvent> EventStream { get; }
}

public abstract class ReactiveViewModel : ReactiveObject, IReactiveViewModel, IDisposable, IEntity
{
    public string EntityId { get; }

    private readonly BehaviorSubject<IEvent> eventSubject;

    public IObservable<IEvent> EventStream => eventSubject;

    protected ReactiveViewModel()
    {
        EntityId = Guid.NewGuid().ToString();
        eventSubject = new BehaviorSubject<IEvent>(IEvent.Empty);
        
        this.Register();
    }

    protected void RaiseEvent(IEvent evt)
    {
        eventSubject.OnNext(evt);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;
        
        this.Forget();

        eventSubject.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}