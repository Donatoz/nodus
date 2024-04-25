namespace Nodus.DI.Factories;

public interface IFactoryProvider<out T>
{
    TFactory GetFactory<TFactory>();
}