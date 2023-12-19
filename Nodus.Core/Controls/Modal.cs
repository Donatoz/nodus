using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Nodus.Core.Controls;

public abstract class Modal : UserControl
{
    public static readonly RoutedEvent<ModalStateEventArgs> ModalStateChangedEvent = 
        RoutedEvent.Register<Modal, ModalStateEventArgs>("OnModalStateChanged", RoutingStrategies.Bubble);
}

public class ModalStateEventArgs : RoutedEventArgs
{
    public bool IsOpened { get; }

    public ModalStateEventArgs(bool isOpened)
    {
        IsOpened = isOpened;
    }
}