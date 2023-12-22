using System;
using System.Collections.Generic;

namespace Nodus.Core.Entities;

public static class EntityMaster
{
    private static readonly IDictionary<IEntity, EntityVirtualState> states = 
        new Dictionary<IEntity, EntityVirtualState>();
    
    public static ICollection<IEntity> RegisteredEntities => states.Keys;
    
    public static void Register(this IEntity entity)
    {
        if (entity.IsRegistered())
        {
            throw new ArgumentException($"Entity with id ({entity.EntityId}) is already registered");
        }
        
        states[entity] = new EntityVirtualState();
    }

    public static void Forget(this IEntity entity)
    {
        if (!entity.IsRegistered())
        {
            throw new ArgumentException("Entity was not registered");
        }

        states[entity].Dispose();
        states.Remove(entity);
    }

    public static T AddComponent<T>(this IEntity entity, T component) where T : IEntityComponent
    {
        ValidateEntity(entity);
        states[entity].AddComponent(component);

        return component;
    }

    public static void RemoveComponent<T>(this IEntity entity) where T : IEntityComponent
    {
        ValidateEntity(entity);
        states[entity].RemoveComponent<T>();
    }

    public static bool TryGetComponent<T>(this IEntity entity, out T component) where T : IEntityComponent
    {
        ValidateEntity(entity);
        return states[entity].TryGetComponent(out component);
    }

    public static T? TryGetComponent<T>(this IEntity entity) where T : IEntityComponent
    {
        ValidateEntity(entity);
        return TryGetComponent(entity, out T component) ? component : default;
    }

    public static bool TryGetGeneric<T>(this IEntity entity, out T generic)
    {
        ValidateEntity(entity);
        return states[entity].TryGetGeneric(out generic);
    }

    public static T? TryGetGeneric<T>(this IEntity entity)
    {
        ValidateEntity(entity);
        return TryGetGeneric(entity, out T g) ? g : default;
    }

    private static void ValidateEntity(IEntity entity)
    {
        if (!entity.IsRegistered())
        {
            throw new ArgumentException("Entity is not registered");
        }
    }

    public static bool IsRegistered(this IEntity entity)
    {
        return states.ContainsKey(entity);
    }
}