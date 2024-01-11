using Avalonia;
using Avalonia.Input;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace Nodus.Core.Behaviors;

public class AutoFocusBehavior : Behavior<InputElement>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        
        if (AssociatedObject == null) return;

        if (AssociatedObject.IsAttachedToVisualTree())
        {
            PerformFocus();
        }
        else
        {
            AssociatedObject.AttachedToVisualTree += OnObjectAttached;
        }
    }

    private void OnObjectAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        AssociatedObject!.AttachedToVisualTree -= OnObjectAttached;
        PerformFocus();
    }

    private void PerformFocus()
    {
        AssociatedObject?.Focus();
    }
}