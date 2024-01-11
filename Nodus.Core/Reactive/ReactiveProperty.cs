using System;
using System.ComponentModel;
using System.Reactive.Subjects;
using ReactiveUI;

namespace Nodus.Core.Reactive;

public interface IReactiveProperty<out T> : IReactiveObject
{
    T Value { get; }
    IObservable<T> AlterationStream { get; }
    void Invalidate();
}

public class ReactiveProperty<T> : IReactiveProperty<T>, IDisposable
{
    protected readonly BehaviorSubject<T> subject;

    public T Value => subject.Value;
    public IObservable<T> AlterationStream => subject;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangingEventHandler? PropertyChanging;
    
    public ReactiveProperty(T value = default)
    {
        subject = new BehaviorSubject<T>(value);
    }

    public void RaisePropertyChanging(PropertyChangingEventArgs args)
    {
        PropertyChanging?.Invoke(this, args);
    }

    public void RaisePropertyChanged(PropertyChangedEventArgs args)
    {
        PropertyChanged?.Invoke(this, args);
    }

    public void Invalidate()
    {
        RaisePropertyChanging(new PropertyChangingEventArgs(nameof(Value)));
        subject.OnNext(Value);
        RaisePropertyChanged(new PropertyChangedEventArgs(nameof(Value)));
    }

    public void Dispose()
    {
        subject.Dispose();
    }
}

public class MutableReactiveProperty<T> : ReactiveProperty<T>
{
    public T MutableValue
    {
        get => Value;
        set => SetValue(value);
    }
    
    public MutableReactiveProperty(T value = default) : base(value)
    {
    }
    
    public virtual void SetValue(T value)
    {
        RaisePropertyChanging(new PropertyChangingEventArgs(nameof(Value)));
        subject.OnNext(value);
        RaisePropertyChanged(new PropertyChangedEventArgs(nameof(Value)));
    }
}