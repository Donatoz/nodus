using System.Reactive;
using ReactiveUI;

namespace Nodus.ObjectDescriptor.ViewModels;

public class ExposedCollectionItemViewModel
{
    public ExposedValueViewModel Value { get; }
    public ReactiveCommand<Unit, Unit> DeleteSelf { get; }

    private readonly ExposedCollectionViewModel collection;
    
    public ExposedCollectionItemViewModel(ExposedValueViewModel value, ExposedCollectionViewModel collection)
    {
        Value = value;
        this.collection = collection;
        
        DeleteSelf = ReactiveCommand.Create(DeleteSelfImpl);
    }

    private void DeleteSelfImpl()
    {
        collection.Remove.Execute(this).Subscribe();
    }
}