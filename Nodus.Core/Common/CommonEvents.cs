namespace Nodus.Core.Common;

public readonly struct CollectionChangedEvent<T> : IEvent
{
    public T Item { get; }
    public bool Added { get; }

    public CollectionChangedEvent(T item, bool added)
    {
        Item = item;
        Added = added;
    }
}