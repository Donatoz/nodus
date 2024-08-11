using System.Reactive;
using ReactiveUI;

namespace Nodus.ObjectDescriptor.ViewModels;

public class ExposedMethodViewModel : ExposedMemberViewModel
{
    public ReactiveCommand<Unit, Unit> Invoke { get; }

    protected IExposedMethod ExposedMethod => (IExposedMethod)Exposed;
    
    public ExposedMethodViewModel(IExposedMethod exposed) : base(exposed)
    {
        Invoke = ReactiveCommand.Create(InvokeImpl);
    }

    private void InvokeImpl()
    {
        ExposedMethod.Invoke();
    }
}