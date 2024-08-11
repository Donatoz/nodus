using System.Collections;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using Nodus.Core.Extensions;
using Nodus.ObjectDescriptor.Factories;
using ReactiveUI;

namespace Nodus.ObjectDescriptor.ViewModels;

public class ExposedCollectionViewModel : ExpandableExposedViewModel
{
    public virtual bool CanCreateNewInstances => 
        ExposedValue.Header.MemberType.GetConstructor(Type.EmptyTypes) != null
        || ExposedValue.Header.MemberType.IsValueType;
    public virtual bool CanCreateNewItems => ItemType.GetConstructor(Type.EmptyTypes) != null || ItemType.IsValueType;

    public ReadOnlyObservableCollection<ExposedCollectionItemViewModel>? Items => items;
    
    public ReactiveCommand<Unit, Unit> TryCreateNew { get; }
    public ReactiveCommand<Unit, Unit> AddNewItem { get; }
    public ReactiveCommand<ExposedCollectionItemViewModel, Unit> Remove { get; }
    
    private ReadOnlyObservableCollection<ExposedCollectionItemViewModel>? items;
    
    private readonly SourceList<IExposedValue> exposedItems;
    private readonly IExposedMemberViewModelFactory memberFactory;

    protected Type ItemType { get; private set; } = typeof(object);

    public ExposedCollectionViewModel(IExposedValue exposed, IExposedMemberViewModelFactory? memberFactory = null) : base(exposed)
    {
        exposedItems = new SourceList<IExposedValue>();
        this.memberFactory = memberFactory ?? ExposedMemberViewModelFactory.Default;
        
        this.WhenActivated(d =>
        {
            exposedItems.Connect()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Transform(x => this.memberFactory.CreateExposedValueViewModel(x))
                .Transform(CreateItemViewModel)
                .Bind(out items)
                .Do(_ => UpdateItemsNames())
                .Subscribe()
                .DisposeWith(d);
            
            d.Add(Disposable.Create(() => exposedItems.Items.OfType<IDisposable>().DisposeAll()));
        });

        TryCreateNew = ReactiveCommand.Create(TryCreateNewImpl);
        AddNewItem = ReactiveCommand.Create(AddNewItemImpl);
        Remove = ReactiveCommand.Create<ExposedCollectionItemViewModel>(RemoveImpl);
    }

    private void TryCreateNewImpl()
    {
        if (!CanCreateNewInstances) return;

        CurrentValue = System.Activator.CreateInstance(ExposedValue.Header.MemberType);
    }

    private void AddNewItemImpl()
    {
        if (!CanCreateNewItems) return;
        
        if (CurrentValue is IList l)
        {
            var idx = l.Count;
            var item = System.Activator.CreateInstance(ItemType);
            
            if (item == null) return;

            l.Add(item);
            
            exposedItems.Add(ExposeItem(item, x =>
            {
                l.RemoveAt(idx);
                l.Insert(idx, x);
            }));
        }
    }

    private void RemoveImpl(ExposedCollectionItemViewModel item)
    {
        var exp = exposedItems.Items.FirstOrDefault(x => item.Value.Exposed == x);

        if (exp == null)
        {
            throw new Exception($"Failed to remove exposed item: {item.Value.Exposed.Header.MemberName}. Exposed primitive not found.");
        }

        exposedItems.Remove(exp);

        if (exp is IDisposable d)
        {
            d.Dispose();
        }
    }

    protected override void ChangeValue(object? value)
    {
        base.ChangeValue(value);
        
        exposedItems.Items.OfType<IDisposable>().DisposeAll();
        exposedItems.Clear();

        DeduceItemType();
        
        if (value is IList l)
        {
            for (var i = 0; i < l.Count; i++)
            {
                var item = l[i];
                var idx = i;
                
                if (item == null) continue;
                
                exposedItems.Add(ExposeItem(item, x =>
                {
                    l.RemoveAt(idx);
                    l.Insert(idx, x);
                }));
            }
        }
    }

    private void DeduceItemType()
    {
        if (CurrentValue == null) return;

        var type = CurrentValue.GetType();

        ItemType = type.IsGenericType ? type.GetGenericArguments().First() : typeof(object);
    }

    private void UpdateItemsNames()
    {
        if (Items == null) return;
        
        for (var i = 0; i < Items.Count; i++)
        {
            Items[i].Value.Label = $"Item {i}";
        }
    }

    private IExposedValue ExposeItem(object item, Action<object?> originMutator)
    {
        return new ExposedPrimitive(new ExposureHeader { MemberName = string.Empty, MemberType = item.GetType() }, item, originMutator);
    }

    private ExposedCollectionItemViewModel CreateItemViewModel(ExposedValueViewModel item)
    {
        return new ExposedCollectionItemViewModel(item, this);
    }
}