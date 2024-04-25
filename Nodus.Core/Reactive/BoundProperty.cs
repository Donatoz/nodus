using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Nodus.Core.Extensions;
using ReactiveUI;

namespace Nodus.Core.Reactive;

public class BoundProperty<T> : IReactiveObject, IDisposable
{
    private T value;
    public T Value
    {
        get => value;
        set => SetValue(value);
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangingEventHandler? PropertyChanging;
    
    private CompositeDisposable alterationContracts;
    private readonly Func<T> getter;
    
    public BoundProperty(Func<T> getter, params IReactiveObject[] sources)
    {
        alterationContracts = new CompositeDisposable();
        this.getter = getter;

        sources.ForEach(AddSource);
        
        UpdateValue();
    }

    private void UpdateValue()
    {
        SetValue(getter.Invoke());
    }

    protected virtual void OnAlteration(object? sender, PropertyChangedEventArgs args)
    {
        UpdateValue();
    }

    public void AddSource(IReactiveObject source)
    {
        source.PropertyChanged += OnAlteration;
        alterationContracts.Add(Disposable.Create(() => source.PropertyChanged -= OnAlteration));
    }

    public void ClearSources()
    {
        alterationContracts.Dispose();
        alterationContracts = new CompositeDisposable();
    }

    private void SetValue(T value)
    {
        RaisePropertyChanging(new PropertyChangingEventArgs(nameof(Value)));
        this.value = value;
        RaisePropertyChanged(new PropertyChangedEventArgs(nameof(Value)));
    }
    
    public void RaisePropertyChanging(PropertyChangingEventArgs args)
    {
        PropertyChanging?.Invoke(this, args);
    }

    public void RaisePropertyChanged(PropertyChangedEventArgs args)
    {
        PropertyChanged?.Invoke(this, args);
    }

    public void Dispose()
    {
        alterationContracts.Dispose();
    }

    public override string ToString() => Value?.ToString() ?? string.Empty;
}

public sealed class NotifyingBoundProperty<T> : BoundProperty<T>
{
    private readonly ISubject<T> alterationSubject;
    public IObservable<T> AlterationStream => alterationSubject;
    
    public NotifyingBoundProperty(Func<T> getter, params IReactiveObject[] sources) : base(getter, sources)
    {
        alterationSubject = new BehaviorSubject<T>(getter.Invoke());
    }

    protected override void OnAlteration(object? sender, PropertyChangedEventArgs args)
    {
        base.OnAlteration(sender, args);
        alterationSubject.OnNext(Value);
    }
}

public static class BoundPropertyEx
{
    public static BoundProperty<T> ToBound<T>(this IReactiveProperty<T> property, [CallerMemberName]string? propName = null)
    {
        return new BoundProperty<T>(() => property.Value, property);
    }
    
    public static BoundProperty<TGet> ToBound<T, TGet>(this IReactiveProperty<T> property, Func<TGet> getter, [CallerMemberName]string? propName = null)
    {
        return new BoundProperty<TGet>(getter, property);
    }
    
    public static NotifyingBoundProperty<T> ToNotifying<T>(this IReactiveProperty<T> property, [CallerMemberName]string? propName = null)
    {
        return new NotifyingBoundProperty<T>(() => property.Value, property);
    }
}