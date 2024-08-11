using ReactiveUI;

namespace Nodus.ObjectDescriptor.ViewModels;

public class ExposedMemberViewModel : ReactiveObject
{
    public string Label
    {
        get => label;
        set => this.RaiseAndSetIfChanged(ref label, value);
    }
    
    public IExposed Exposed { get; }


    private string label;

    public ExposedMemberViewModel(IExposed exposed)
    {
        Exposed = exposed;
        label = exposed.Header.MemberName;
    }
}