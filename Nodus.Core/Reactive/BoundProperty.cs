using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using ReactiveUI;

namespace Nodus.Core.Reactive;

public class BoundProperty<T> : IReactiveObject, IDisposable
{
    public T Value { get; private set; }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangingEventHandler? PropertyChanging;
    
    private readonly CompositeDisposable contract;
    private readonly Func<T> getter;
    private readonly Action<T> setter;
    
    public BoundProperty(Func<T> getter, params IReactiveObject[] sources)
    {
        contract = new CompositeDisposable();
        this.getter = getter;

        foreach (var s in sources)
        {
            s.PropertyChanged += OnAlteration;
            contract.Add(Disposable.Create(() => s.PropertyChanged -= OnAlteration));
        }
        
        OnAlteration(null, null);
    }

    protected virtual void OnAlteration(object? sender, PropertyChangedEventArgs args)
    {
        var value = getter.Invoke();
        SetValue(value);
    }

    public void SetValue(T value)
    {
        RaisePropertyChanging(new PropertyChangingEventArgs(nameof(Value)));
        Value = value;
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
        contract.Dispose();
    }

    public override string ToString() => Value?.ToString() ?? string.Empty;
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
}