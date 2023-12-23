namespace Nodus.Core.Entities;

/// <summary>
/// Represents a container that holds a single value of a specified type.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
/// <remarks>
/// <para>
/// The <see cref="ValueContainer{T}"/> struct provides a readonly container for storing a single value of any type.
/// </para>
/// </remarks>
public readonly struct ValueContainer<T> : IEntityComponent 
{
    /// <summary>
    /// The value of the property.
    /// </summary>
    public T Value { get; }

    public ValueContainer(T value)
    {
        Value = value;
    }
}