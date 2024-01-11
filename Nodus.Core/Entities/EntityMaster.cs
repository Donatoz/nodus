using System;
using System.Collections.Generic;

namespace Nodus.Core.Entities;

/// <summary>
/// A static class that manages the registration and manipulation of entities.
/// </summary>
public static class EntityMaster
{
    private static readonly IDictionary<IEntity, EntityVirtualState> states = 
        new Dictionary<IEntity, EntityVirtualState>();

    /// <summary>
    /// The collection of registered entities.
    /// </summary>
    public static IEnumerable<IEntity> RegisteredEntities => states.Keys;

    /// <summary>
    /// Register the specified entity.
    /// </summary>
    /// <param name="entity">The entity to be registered.</param>
    /// <exception cref="ArgumentException">Thrown if the entity is already registered.</exception>
    public static void Register(this IEntity entity)
    {
        if (entity.IsRegistered())
        {
            throw new ArgumentException($"Entity with id ({entity.EntityId}) is already registered");
        }
        
        states[entity] = new EntityVirtualState();
    }

    /// <summary>
    /// Remove an entity from the internal states dictionary, destroying attached state as well.
    /// </summary>
    /// <param name="entity">The entity to forget.</param>
    /// <exception cref="ArgumentException">Thrown when the entity is not registered.</exception>
    public static void Forget(this IEntity entity)
    {
        if (!entity.IsRegistered())
        {
            throw new ArgumentException("Entity was not registered");
        }

        states[entity].Dispose();
        states.Remove(entity);
    }

    /// <summary>
    /// Add a component to an entity.
    /// </summary>
    /// <typeparam name="T">The type of component being added.</typeparam>
    /// <param name="entity">The entity to add the component to.</param>
    /// <param name="component">The component to add.</param>
    /// <returns>The added component.</returns>
    public static T AddComponent<T>(this IEntity entity, T component) where T : IEntityComponent
    {
        ValidateEntity(entity);
        states[entity].AddComponent(component);

        return component;
    }

    /// <summary>
    /// Remove a component from the specified entity.
    /// </summary>
    /// <typeparam name="T">The type of the component to remove.</typeparam>
    /// <param name="entity">The entity from which to remove the component.</param>
    public static void RemoveComponent<T>(this IEntity entity) where T : IEntityComponent
    {
        ValidateEntity(entity);
        states[entity].RemoveComponent<T>();
    }

    /// <summary>
    /// Attempt to retrieve a component of type <see cref="T"/> from the specified entity.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <param name="entity">The entity to retrieve the component from.</param>
    /// <param name="component">When this method returns, contains the retrieved component, if found; otherwise, the default value for the component type.</param>
    /// <returns>
    /// True if the component is found and retrieved successfully; otherwise, false.
    /// If the component is found, it is assigned to the <paramref name="component"/> parameter.
    /// If the component is not found, <paramref name="component"/> will be set to the default value for the component type.
    /// </returns>
    public static bool TryGetComponent<T>(this IEntity entity, out T component) where T : IEntityComponent
    {
        ValidateEntity(entity);
        return states[entity].TryGetComponent(out component);
    }

    /// <summary>
    /// Try to get the component of type <see cref="T"/> from the given entity virtual state.
    /// </summary>
    /// <typeparam name="T">The type of the component to get. Must be a <see cref="IEntityComponent"/>.</typeparam>
    /// <param name="entity">The entity to get the component from.</param>
    /// <returns>
    /// An optional instance of the component of type T, if it exists on the entity.
    /// Otherwise, returns null.
    /// </returns>
    public static T? TryGetComponent<T>(this IEntity entity) where T : IEntityComponent
    {
        ValidateEntity(entity);
        return TryGetComponent(entity, out T component) ? component : default;
    }

    /// <summary>
    /// Try to get the generic component associated with the given entity.
    /// </summary>
    /// <typeparam name="T">The type of the generic value.</typeparam>
    /// <param name="entity">The entity to get the generic value for.</param>
    /// <param name="generic">The generic value associated with the entity, if found.</param>
    /// <returns>True if the generic value is found and assigned to the out parameter; otherwise, false.</returns>
    public static bool TryGetGeneric<T>(this IEntity entity, out T generic)
    {
        ValidateEntity(entity);
        return states[entity].TryGetGeneric(out generic);
    }

    /// <summary>
    /// Try to get the first component which is assignable from <typeparamref name="T"/> type from the specified entity.
    /// </summary>
    /// <typeparam name="T">The assignable type of the component.</typeparam>
    /// <param name="entity">The entity to get the generic value from.</param>
    /// <returns>
    /// The generic value of type <typeparamref name="T"/> if it exists in the entity;
    /// otherwise, returns <c>null</c>.
    /// </returns>
    public static T? TryGetGeneric<T>(this IEntity entity)
    {
        ValidateEntity(entity);
        return TryGetGeneric(entity, out T g) ? g : default;
    }

    /// <summary>
    /// Validate an entity.
    /// </summary>
    /// <param name="entity">The entity to be validated.</param>
    /// <exception cref="ArgumentException">Thrown if the entity is not registered.</exception>
    private static void ValidateEntity(IEntity entity)
    {
        if (!entity.IsRegistered())
        {
            throw new ArgumentException("Entity is not registered");
        }
    }

    /// <summary>
    /// Check if the given entity is registered.
    /// </summary>
    /// <param name="entity">The entity to be checked.</param>
    /// <returns>True if the entity is registered; otherwise, false.</returns>
    public static bool IsRegistered(this IEntity entity)
    {
        return states.ContainsKey(entity);
    }
}