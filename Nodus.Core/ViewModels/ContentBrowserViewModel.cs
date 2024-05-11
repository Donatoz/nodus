using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Nodus.Core.Common;
using Nodus.Core.Entities;
using Nodus.Core.Selection;
using ReactiveUI;

namespace Nodus.Core.ViewModels;

public class ContentBrowserViewModel
{
    private readonly ObservableCollection<ContentBrowserItemViewModel> items;
    
    public ObservableCollection<ContentBrowserItemViewModel> Items => items;
    
    public ISelector<ISelectable> ItemSelector { get; }

    private IDisposable contract;

    public ContentBrowserViewModel()
    {
        ItemSelector = new Selector<ISelectable>();

        items = new ObservableCollection<ContentBrowserItemViewModel>(new[]
        {
            CreateItem(),
            CreateItem(),
            CreateItem(),
            CreateItem(),
            CreateItem()
        });
    }

    public void HandleSelection(ContentBrowserItemViewModel item, bool additive)
    {
        if (ItemSelector.CurrentlySelected.Contains(item.SelectableComponent))
        {
            ItemSelector.Deselect(item.SelectableComponent);
            return;
        }
        
        if (!additive)
        {
            ItemSelector.DeselectAll();
        }
        
        ItemSelector.Select(item.SelectableComponent);
    }

    private ContentBrowserItemViewModel CreateItem()
    {
        var item = new ContentBrowserItemViewModel();

        item.AttachDisposable(item.EventStream
            .OfType<ContentBrowserItemViewModel.DeletionEvent>()
            .Subscribe(_ => RemoveItem(item)));

        return item;
    }

    private void RemoveItem(ContentBrowserItemViewModel item)
    {
        item.Dispose();
        Items.Remove(item);
    }
}

public class ContentBrowserItemViewModel : ReactiveViewModel
{
    public ISelectable SelectableComponent { get; }
    public ReactiveCommand<Unit, Unit> Delete { get; }
    
    public ContentBrowserItemViewModel()
    {
        SelectableComponent = this.AddComponent(new SelectionHandler());

        Delete = ReactiveCommand.Create(DeleteImpl);
    }

    private void DeleteImpl()
    {
        RaiseEvent(new DeletionEvent());
    }
    
    public readonly struct DeletionEvent : IEvent { }
}