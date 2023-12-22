using System;
using System.Collections.Generic;
using System.Linq;

namespace Nodus.Core.Entities;

public class EntityVirtualState : IDisposable
{
    private readonly IDictionary<Type, IEntityComponent> components;

    public EntityVirtualState()
    {
        components = new Dictionary<Type, IEntityComponent>();
    }

    public void AddComponent(IEntityComponent component)
    {
        components[component.GetType()] = component;
    }

    public void RemoveComponent<T>() where T : IEntityComponent
    {
        ValidateComponent<T>();

        if (components[typeof(T)] is IDisposable d)
        {
            d.Dispose();
        }
            
        components.Remove(typeof(T));
    }

    public bool TryGetComponent<T>(out T component) where T : IEntityComponent
    {
        if (components.ContainsKey(typeof(T)) && components[typeof(T)] is T c)
        {
            component = c;
            return true;
        }

        component = default;
        return false;
    }

    public bool TryGetGeneric<T>(out T component)
    {
        var c = components.Values.FirstOrDefault(c => c is T);

        if (c != null)
        {
            component = (T) c;
            return true;
        }

        component = default;
        return false;
    }

    private void ValidateComponent<T>() where T : IEntityComponent
    {
        if (!HasComponent<T>())
        {
            throw new ArgumentException("Component is not contained within this state");
        }
    }

    public bool HasComponent<T>() where T : IEntityComponent
    {
        return components.ContainsKey(typeof(T));
    }

    public void Dispose()
    {
        foreach (var c in components.Values.OfType<IDisposable>())
        {
            c.Dispose();
        }
    }
}