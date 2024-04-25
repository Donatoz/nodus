namespace Nodus.DI.Factories;

public interface IFactory<out TOut>
{
    TOut Create();
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