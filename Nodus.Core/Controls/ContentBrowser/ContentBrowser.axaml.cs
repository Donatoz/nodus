using System.Reactive.Disposables;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using Nodus.Core.Extensions;
using Nodus.Core.Selection;
using Nodus.Core.ViewModels;
using ReactiveUI;

namespace Nodus.Core.Controls;

public partial class ContentBrowser : ReactiveUserControl<ContentBrowserViewModel>
{
    public ContentBrowser()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.Items, v => v.ItemsControl.ItemsSource)
                .DisposeWith(d);
        });
        
        AddHandler(ContentBrowserItem.ItemPressedEvent, OnItemPressed);
    }

    private void OnItemPressed(object? sender, SelectablePressedEventArgs<ContentBrowserItem> e)
    {
        ViewModel?.HandleSelection(e.Control.ViewModel.NotNull(), e.Modifiers == KeyModifiers.Shift);
    }
}