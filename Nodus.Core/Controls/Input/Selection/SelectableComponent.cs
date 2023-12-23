using System;
using Avalonia;
using Avalonia.Controls;
using Nodus.Core.ViewModels;

namespace Nodus.Core.Selection;

/// <summary>
/// Represents a selectable component that can be used in an Avalonia UI.
/// </summary>
public sealed class SelectableComponent : AvaloniaObject, IDisposable
{
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<SelectableComponent, bool>(nameof(IsSelected));

    /// <summary>
    /// A value indicating whether the item is selected.
    /// </summary>
    /// <value>
    /// <c>true</c> if the item is selected; otherwise, <c>false</c>.
    /// </value>
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        private set => SetValue(IsSelectedProperty, value);
    }

    private IDisposable? eventContract;
    private readonly Control parent;

    public SelectableComponent(Control parent)
    {
        this.parent = parent;
        parent.DataContextChanged += OnContextChanged;
    }

    private void OnContextChanged(object? sender, EventArgs args)
    {
        eventContract?.Dispose();
        
        if (sender is not Control c) return;
        
        if (c.DataContext is IReactiveViewModel vm)
        {
            eventContract = vm.EventStream.Subscribe(evt =>
            {
                if (evt is SelectionEvent selectionEvent)
                {
                    IsSelected = selectionEvent.IsSelected;

                    parent.ZIndex = IsSelected ? 100 : 0;
                }
            });
        }
    }

    public void Dispose()
    {
        eventContract?.Dispose();
        parent.DataContextChanged -= OnContextChanged;
    }
}