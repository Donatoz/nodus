using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using Nodus.ObjectDescriptor.ViewModels;
using ReactiveUI;

namespace Nodus.ObjectDescriptor.Controls;

public partial class DescribedObjectList : ReactiveUserControl<DescribedObjectViewModel>
{
    public DescribedObjectList()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.ExposedValues, v => v.ValuesItemsControl.ItemsSource)
                .DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.ExposedMethods, v => v.MethodItemsControl.ItemsSource)
                .DisposeWith(d);
        });
    }
}