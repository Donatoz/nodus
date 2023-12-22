namespace Nodus.Core.Entities;

public readonly struct ValueContainer<T> : IEntityComponent 
{
    public T Value { get; }

    public ValueContainer(T value)
    {
        Value = value;
    }
}