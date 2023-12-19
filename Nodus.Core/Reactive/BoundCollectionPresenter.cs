using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using Nodus.Core.Common;
using Nodus.Core.Extensions;

namespace Nodus.Core.Reactive;

public class BoundCollectionPresenter<TCtx, TControl> : IDisposable 
    where TControl : Control 
    where TCtx : class
{
    public Func<Control, TCtx, bool> DestructionPredicate { get; set; }

    private IDisposable? contract;

    private readonly Func<TCtx, TControl> controlFactory;
    private readonly Panel container;
    private readonly Action<TControl>? controlDestroyer;
    
    public BoundCollectionPresenter(Func<TCtx, TControl> controlFactory, 
        Panel container, Action<TControl>? controlDestroyer = null)
    {
        this.controlFactory = controlFactory;
        this.container = container;
        this.controlDestroyer = controlDestroyer;
        DestructionPredicate = (c, ctx) => c.DataContext == ctx;
    }

    public void Subscribe(IObservable<CollectionChangedEvent<TCtx>> stream)
    {
        contract?.Dispose();
        contract = stream.Subscribe(Observer.Create<CollectionChangedEvent<TCtx>>(Update));
    }
    
    public void Repopulate(IEnumerable<TCtx> items)
    {
        container.Children.OfType<TControl>().ForEach(x => container.Children.Remove(x));
        items.ForEach(CreateControl);
    }

    private void Update(CollectionChangedEvent<TCtx> evt)
    {
        if (evt.Added)
        {
            CreateControl(evt.Item);
        }
        else
        {
            RemoveControl(evt.Item);
        }
    }

    private void CreateControl(TCtx ctx)
    {
        var control = controlFactory.Invoke(ctx);
        container.Children.Add(control);
    }

    private void RemoveControl(TCtx ctx)
    {
        var c = container.Children.FirstOrDefault(x => DestructionPredicate.Invoke(x ,ctx));
        
        if (c != null)
        {
            container.Children.Remove(c);

            if (c is TControl control)
            {
                controlDestroyer?.Invoke(control);
            }
        }
    }

    public void Dispose()
    {
        contract?.Dispose();
    }
}