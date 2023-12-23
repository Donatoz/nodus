namespace Nodus.Core.Selection;

/// <summary>
/// Represents an object that can be selected and deselected.
/// </summary>
public interface ISelectable
{
    /// <summary>
    /// Select this item.
    /// </summary>
    void Select();

    /// <summary>
    /// Deselect this item.
    /// </summary>
    void Deselect();
}