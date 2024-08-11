using System.Reactive.Subjects;
using Nodus.Core.Extensions;

namespace Nodus.ObjectDescriptor;

public sealed class ExposedPrimitive : IExposedValue, IDisposable
{
    public object? CurrentValue => valueSubject.Value;
    public ExposureHeader Header { get; }
    public IObservable<object?> ValueStream => valueSubject;

    private readonly BehaviorSubject<object?> valueSubject;
    
    public ExposedPrimitive(ExposureHeader header, object? initialValue = null, params Action<object?>[] bindings)
    {
        Header = header;
        valueSubject = new BehaviorSubject<object?>(initialValue);
        
        bindings.ForEach(x => ValueStream.Subscribe(x));
    }

    public void TryChange(object? value)
    {
        valueSubject.OnNext(value);
    }

    public void Dispose()
    {
        valueSubject.Dispose();
    }

    public override string ToString()
    {
        return $"{Header.MemberName}<{Header.MemberType}>";
    }
}