using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
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

    private readonly ISubject<IEvent> eventSubject;
    private readonly CompositeDisposable disposables;

    public IObservable<IEvent> EventStream => eventSubject;

    protected ReactiveViewModel()
    {
        EntityId = Guid.NewGuid().ToString();
        eventSubject = CreateSubject();
        disposables = new CompositeDisposable();
        
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

        if (eventSubject is IDisposable d)
        {
            d.Dispose();
        }
        
        disposables.Dispose();
    }

    protected virtual ISubject<IEvent> CreateSubject()
    {
        return new BehaviorSubject<IEvent>(IEvent.Empty);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}