using Nodus.Core.Common;

namespace Nodus.Core.Selection;

/// <summary>
/// Represents an event that occurs when an item is selected or unselected.
/// </summary>
public readonly struct SelectionEvent : IEvent
{
    /// <summary>
    /// Sender of the object.
    /// </summary>
    /// <returns>The selectable sender.</returns>
    public ISelectable Sender { get; }

    /// <summary>
    /// A value indicating whether the object was selected.
    /// </summary>
    /// <returns>
    /// true if the object was selected; otherwise (if deselected), false.
    /// </returns>
    public bool IsSelected { get; }

    public SelectionEvent(ISelectable sender, bool isSelected)
    {
        Sender = sender;
        IsSelected = isSelected;
    }
}