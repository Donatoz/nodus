namespace Nodus.Core.Entities;

public interface IContainer<out T> : IEntityComponent
{
    T Value { get; }
}

public static class ContainerExtensions
{
    public static T? TryGetContainedValue<T>(this IEntity e)
    {
        return e.TryGetGeneric(out IContainer<T> c) ? c.Value : default;
    }
}