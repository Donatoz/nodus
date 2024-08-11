using System.Reactive;
using System.Reactive.Disposables;
using ReactiveUI;

namespace Nodus.ObjectDescriptor.ViewModels;

public class ExposedValueViewModel : ExposedMemberViewModel, IActivatableViewModel
{
    public object? CurrentValue
    {
        get => currentValue;
        set => ExposedValue.TryChange(value);
    }
    
    public ViewModelActivator Activator { get; }
    public Type ValueType => ExposedValue.Header.MemberType;
    
    public ReactiveCommand<Unit, Unit> Reset { get; }

    protected IExposedValue ExposedValue => (IExposedValue)Exposed;

    private object? currentValue;

    public ExposedValueViewModel(IExposedValue exposed) : base(exposed)
    {
        CurrentValue = ExposedValue.CurrentValue;
        Activator = new ViewModelActivator();

        this.WhenActivated(d =>
        {
            ExposedValue.ValueStream.Subscribe(ChangeValue)
                .DisposeWith(d);
        });

        Reset = ReactiveCommand.Create(ResetImpl);
    }

    private void ResetImpl()
    {
        CurrentValue = ValueType.IsValueType ? System.Activator.CreateInstance(ValueType) : null;
    }

    protected virtual void ChangeValue(object? value)
    {
        currentValue = value;
        this.RaisePropertyChanged(nameof(CurrentValue));
    }

    public void Invalidate()
    {
        ChangeValue(ExposedValue.CurrentValue);
    }
}