using Nodus.Core.Common;

namespace Nodus.Core.Selection;

public readonly struct SelectionEvent : IEvent
{
    public ISelectable Sender { get; }
    public bool IsSelected { get; }

    public SelectionEvent(ISelectable sender, bool isSelected)
    {
        Sender = sender;
        IsSelected = isSelected;
    }
}