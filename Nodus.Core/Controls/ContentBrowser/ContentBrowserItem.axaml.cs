using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using Nodus.Core.Selection;
using Nodus.Core.ViewModels;
using ReactiveUI;
using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Nodus.Core.Entities;
using Nodus.Core.Extensions;
using Nodus.Core.Utility;

namespace Nodus.Core.Controls;

public partial class ContentBrowserItem : ReactiveUserControl<ContentBrowserItemViewModel>
{
    public static readonly RoutedEvent<SelectablePressedEventArgs<ContentBrowserItem>> ItemPressedEvent;
    public static readonly RoutedEvent<ControlRoutedEventArgs<ContentBrowserItem>> ItemDeletionEvent;

    public ContentBrowserItem()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            ViewModel!.TryGetComponent<SelectionHandler>()?.SelectionStream
                .Select(x => x.IsSelected)
                .Subscribe(OnSelectionChanged)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.Delete, v => v.DeleteMenuItem)
                .DisposeWith(d);
        });
    }

    static ContentBrowserItem()
    {
        ItemPressedEvent = RoutedEvent.Register<ContentBrowserItem, SelectablePressedEventArgs<ContentBrowserItem>>(nameof(ItemPressedEvent), RoutingStrategies.Bubble);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        
        if (e.Source is not Control) return;

        RaiseEvent(new SelectablePressedEventArgs<ContentBrowserItem>(this, e.KeyModifiers) {RoutedEvent = ItemPressedEvent});
    }

    private void OnSelectionChanged(bool selected)
    {
        Trace.WriteLine($"------------- Selection: {selected}");
        Container.SwitchClass("active", selected);
    }
}