namespace Nodus.DI.Factories;

public interface IFactory<out TOut>
{
    TOut Create();
}

public readonly struct AnonymousFactory<TOut> : IFactory<TOut>
{
    private readonly Func<TOut> factory;

    public AnonymousFactory(Func<TOut> factory)
    {
        this.factory = factory;
    }

    public TOut Create() => factory.Invoke();
    
    public static implicit operator AnonymousFactory<TOut>(Func<TOut> factory) => new(factory);
}

public interface IFactory<in TIn, out TOut>
{
    TOut Create(TIn arg1);
}

public interface IFactory<in TIn1, in TIn2, out TOut>
{
    TOut Create(TIn1 arg1, TIn2 arg2);
}

public interface IFactory<in TIn1, in TIn2, in TIn3, out TOut>
{
    TOut Create(TIn1 arg1, TIn2 arg2, TIn3 arg3);
}

public interface IFactory<in TIn1, in TIn2, in TIn3, in TIn4, out TOut>
{
    TOut Create(TIn1 arg1, TIn2 arg2, TIn3 arg3, TIn4 arg4);
}