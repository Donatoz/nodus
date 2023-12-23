using System;
using System.Collections.Generic;
using System.Linq;

namespace Nodus.Core.Entities;

/// <summary>
/// Represents a virtual state of an entity.
/// </summary>
public sealed class EntityVirtualState : IDisposable
{
    private readonly IDictionary<Type, IEntityComponent> components;

    public EntityVirtualState()
    {
        components = new Dictionary<Type, IEntityComponent>();
    }

    /// <summary>
    /// Add a component to the entity.
    /// </summary>
    /// <param name="component">The component to add.</param>
    public void AddComponent(IEntityComponent component)
    {
        components[component.GetType()] = component;
    }

    /// <summary>
    /// Remove the specified component from the entity.
    /// </summary>
    /// <typeparam name="T">The type of component to remove.</typeparam>
    /// <remarks>
    /// This method removes the specified component from the entity. The component must implement the <see cref="IEntityComponent"/> interface.
    /// If the component implements the <see cref="IDisposable"/> interface, it will be disposed before removal.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if the specified component type is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the specified component type is not a valid component type.</exception>
    public void RemoveComponent<T>() where T : IEntityComponent
    {
        ValidateComponent<T>();

        if (components[typeof(T)] is IDisposable d)
        {
            d.Dispose();
        }
            
        components.Remove(typeof(T));
    }

    /// <summary>
    /// Try to retrieve a component of type T from the dictionary of components.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <param name="component">An out parameter that will contain the retrieved component if it exists.</param>
    /// <returns>Returns true if the component was found and retrieved successfully, false otherwise.</returns>
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

    /// <summary>
    /// Try to get a specific component of type <typeparamref name="T"/> from the components collection.
    /// </summary>
    /// <typeparam name="T">The type of the component.</typeparam>
    /// <param name="component">The output parameter to store the found component.</param>
    /// <returns>True if a component of type T is found, otherwise false.</returns>
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

    /// <summary>
    /// Validate if a component of type T is present within the state.
    /// </summary>
    /// <typeparam name="T">The type of the component.</typeparam>
    /// <exception cref="ArgumentException">Thrown when the component is not contained within the state.</exception>
    private void ValidateComponent<T>() where T : IEntityComponent
    {
        if (!HasComponent<T>())
        {
            throw new ArgumentException("Component is not contained within this state");
        }
    }

    /// <summary>
    /// Check if the entity has a component of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the component to check for.</typeparam>
    /// <returns>True if the entity has a component of type T; otherwise, false.</returns>
    public bool HasComponent<T>() where T : IEntityComponent
    {
        return components.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Dispose all the disposable components within the dictionary.
    /// </summary>
    /// <remarks>
    /// This method disposes of all the components in the dictionary that implement the IDisposable interface.
    /// It iterates through the dictionary values and checks if the value is an instance of the IDisposable interface.
    /// If so, it calls the Dispose() method on that component.
    /// </remarks>
    /// <seealso cref="IDisposable"/>
    public void Dispose()
    {
        foreach (var c in components.Values.OfType<IDisposable>())
        {
            c.Dispose();
        }
    }
}