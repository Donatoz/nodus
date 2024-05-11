using System;
using System.Reactive.Subjects;
using Nodus.Core.Selection;

namespace Nodus.Core.Entities;

public class SelectionHandler : IEntityComponent, ISelectable, IDisposable
{
    private readonly Subject<SelectionEvent> subject;
    
    public IObservable<SelectionEvent> SelectionStream => subject;
    
    public SelectionHandler()
    {
        subject = new Subject<SelectionEvent>();
    }

    public void Select() => subject.OnNext(new SelectionEvent(this, true));
    public void Deselect() => subject.OnNext(new SelectionEvent(this, false));

    public void Dispose()
    {
        subject.Dispose();
    }
}