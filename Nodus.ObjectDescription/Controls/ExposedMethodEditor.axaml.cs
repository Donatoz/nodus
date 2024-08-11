using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using Nodus.ObjectDescriptor.ViewModels;
using ReactiveUI;

namespace Nodus.ObjectDescriptor.Controls;

public partial class ExposedMethodEditor : ReactiveUserControl<ExposedMethodViewModel>
{
    public ExposedMethodEditor()
    {
        InitializeComponent();
        
        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.Label, v => v.NameText.Text)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.Invoke, v => v.InvokeButton)
                .DisposeWith(d);
        });
    }
}