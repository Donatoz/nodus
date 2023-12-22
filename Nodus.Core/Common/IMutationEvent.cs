namespace Nodus.Core.Common;

public interface IMutationEvent<out T> : IEvent where T : class
{
    T MutatedValue { get; }
}

public readonly struct MutationEvent<T> : IMutationEvent<T> where T : class
{
    public T MutatedValue { get; }

    public MutationEvent(T mutatedValue)
    {
        MutatedValue = mutatedValue;
    }
}