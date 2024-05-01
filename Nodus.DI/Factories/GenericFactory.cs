namespace Nodus.DI.Factories;

public abstract class GenericFactory<TParam, TOut> : IFactory<TParam, TOut>
{
    private readonly ISet<SubFactory> subFactories;

    public GenericFactory()
    {
        subFactories = new HashSet<SubFactory>();
    }

    public TOut Create(TParam arg)
    {
        var subFactory = subFactories.FirstOrDefault(x => x.ArgumentType.IsInstanceOfType(arg));
        
        if (subFactory.Equals(default))
        {
            throw new ArgumentException($"Failed to create object for template ({arg}): sub-factory not found.");
        }

        return subFactory.Factory.Invoke(arg);
    }
    
    protected void RegisterSubFactory(SubFactory subFactory)
    {
        subFactories.Add(subFactory);
    }
    
    protected readonly record struct SubFactory(
        Type ArgumentType,
        Func<TParam, TOut> Factory);
} 