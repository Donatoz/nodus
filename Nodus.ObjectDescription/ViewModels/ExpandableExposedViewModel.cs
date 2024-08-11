using System.Diagnostics;
using System.Reactive;
using ReactiveUI;

namespace Nodus.ObjectDescriptor.ViewModels;

public class ExpandableExposedViewModel : ExposedValueViewModel
{
    private bool isExpanded;
    
    public bool IsExpanded { get => isExpanded; private set => this.RaiseAndSetIfChanged(ref isExpanded, value); }
    
    public ReactiveCommand<Unit, Unit> Expand { get; }
    
    public ExpandableExposedViewModel(IExposedValue exposed) : base(exposed)
    {
        Expand = ReactiveCommand.Create(ExpandImpl);
    }
    
    private void ExpandImpl()
    {
        IsExpanded = !IsExpanded;
    }
}