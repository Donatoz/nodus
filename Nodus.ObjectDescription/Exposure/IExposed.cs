namespace Nodus.ObjectDescriptor;

public interface IExposed
{
    ExposureHeader Header { get; }
}

public interface IExposedValue : IExposed
{
    object? CurrentValue { get; }
    IObservable<object?> ValueStream { get; }
    
    void TryChange(object? value);
}

public interface IExposedMethod : IExposed
{
    object? Invoke(object?[]? args = null);
}