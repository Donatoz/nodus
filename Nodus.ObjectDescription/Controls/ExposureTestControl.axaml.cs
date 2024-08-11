using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using Nodus.ObjectDescriptor.ViewModels;
using ReactiveUI;

namespace Nodus.ObjectDescriptor.Controls;

public partial class ExposureTestControl : ReactiveUserControl<ExposureTestVm>
{
    public ExposureTestControl()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.DescribedObject.ValueFour, v => v.TestText.Text, 
                    x => $"V = ")
                .DisposeWith(d);
        });
    }
}