using System;
using Avalonia;
using Avalonia.Controls;
using Nodus.Core.ViewModels;

namespace Nodus.Core.Selection;

public sealed class SelectableComponent : AvaloniaObject, IDisposable
{
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<SelectableComponent, bool>(nameof(IsSelected));
    
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