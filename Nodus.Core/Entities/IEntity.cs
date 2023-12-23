namespace Nodus.Core.Entities;

/// <summary>
/// Represents an entity with an unique identifier.
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