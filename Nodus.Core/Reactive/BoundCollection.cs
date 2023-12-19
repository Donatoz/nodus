using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using Nodus.Core.Common;
using Nodus.Core.Extensions;

namespace Nodus.Core.Reactive;

public class BoundCollection<TSource, TProduced> : IDisposable
{
    public IObservable<CollectionChangedEvent<TProduced>> AlterationStream => subject;
    public IEnumerable<TProduced> Items => productionBindings.Values;

    private readonly IDictionary<TSource, TProduced> productionBindings;
    private readonly ISet<TSource> tempoState;
    private readonly Subject<CollectionChangedEvent<TProduced>> subject;
    private readonly IDisposable contract;
    private readonly Func<TSource, TProduced> factory;
    
    public BoundCollection(IReactiveProperty<IEnumerable<TSource>> source, Func<TSource, TProduced> factory)
    {
        tempoState = new HashSet<TSource>();
        productionBindings = new Dictionary<TSource, TProduced>();
        subject = new Subject<CollectionChangedEvent<TProduced>>();
        this.factory = factory;

        contract = source.AlterationStream.Subscribe(Observer.Create<IEnumerable<TSource>>(OnAlteration));
    }

    private void OnAlteration(IEnumerable<TSource> newState)
    {
        newState.Except(tempoState).ForEach(RaiseAddition);
        tempoState.Except(newState).ForEach(RaiseRemoval);
        
        tempoState.Clear();
        newState.ForEach(x => tempoState.Add(x));
    }

    private void RaiseAddition(TSource item)
    {
        var newItem = factory.Invoke(item);
        subject.OnNext(new CollectionChangedEvent<TProduced>(newItem, true));

        productionBindings[item] = newItem;
    }

    private void RaiseRemoval(TSource item)
    {
        if (!productionBindings.ContainsKey(item))
        {
            throw new Exception($"Failed to raise removal event: no binding found for source element {item}");
        }
        
        subject.OnNext(new CollectionChangedEvent<TProduced>(productionBindings[item], false));
        productionBindings.Remove(item);
    }

    public void Dispose()
    {
        subject.Dispose();
        contract.Dispose();
    }
}