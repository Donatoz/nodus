using System.Reactive;
using ReactiveUI;

namespace Nodus.ObjectDescriptor.ViewModels;

public class ExposedCompoundViewModel : ExpandableExposedViewModel
{
    public virtual bool CanCreateNewInstances => 
        ExposedValue.Header.MemberType.GetConstructor(Type.EmptyTypes) != null
        || ExposedValue.Header.MemberType.IsValueType;

    public ReactiveCommand<Unit, Unit> TryCreateNew { get; }
    
    public DescribedObjectViewModel DescribedExposedValue { get; }
    
    public ExposedCompoundViewModel(IExposedValue exposed) : base(exposed)
    {
        DescribedExposedValue = new DescribedObjectViewModel();
        
        this.WhenActivated(d =>
        {
            d.Add(DescribedExposedValue);
        });
        
        TryCreateNew = ReactiveCommand.Create(TryCreateNewImpl);
    }

    protected override void ChangeValue(object? value)
    {
        base.ChangeValue(value);
        
        DescribedExposedValue.ChangeTarget(value);
    }

    private void TryCreateNewImpl()
    {
        if (!CanCreateNewInstances) return;

        CurrentValue = System.Activator.CreateInstance(ExposedValue.Header.MemberType);
    }
}