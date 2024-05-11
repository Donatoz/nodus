using Avalonia.Interactivity;

namespace Nodus.Core.Utility;

public class ControlRoutedEventArgs<TControl> : RoutedEventArgs
{
    public TControl Control { get; }

    public ControlRoutedEventArgs(TControl control)
    {
        Control = control;
    }
}