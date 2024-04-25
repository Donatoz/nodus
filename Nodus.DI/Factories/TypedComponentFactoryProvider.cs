namespace Nodus.DI.Factories;

public interface ITypeCachedComponentFactoryProvider
{
    void RegisterFactory(Type factoryType, object factory);
}

public class TypedComponentFactoryProvider<T> : IFactoryProvider<T>, ITypeCachedComponentFactoryProvider
{
    private readonly IDictionary<Type, object> factories;

    public TypedComponentFactoryProvider()
    {
        factories = new Dictionary<Type, object>();
    }

    public void RegisterFactory(Type factoryType, object factory)
    {
        factories[factoryType] = factory;
    }
    
    public TFactory GetFactory<TFactory>()
    {
        if (!factories.ContainsKey(typeof(TFactory)))
        {
            throw new ArgumentException($"Failed to provide factory of type ({typeof(T)}): factory not registered.");
        }

        if (factories[typeof(TFactory)] is not TFactory f)
        {
            throw new Exception($"Requested factory is not compatible to provided ({typeof(TFactory)}) type. Actual: {factories[typeof(TFactory)].GetType()}");
        }

        return f;
    }
}