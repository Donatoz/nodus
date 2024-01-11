namespace Nodus.Core.Entities;

/// <summary>
/// Represents an object with a unique identifier.
/// The identifier is used to bind the entity to a specific virtual state which contains extension components.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Gets the unique identifier of the entity.
    /// </summary>
    /// <value>
    /// The entity ID.
    /// </value>
    string EntityId { get; }
}