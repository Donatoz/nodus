namespace Nodus.DI.Factories;

public interface IComponentFactoryProvider<out T>
{
    TFactory GetFactory<TFactory>();
}