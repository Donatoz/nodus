using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using Material.Icons;
using Nodus.ObjectDescriptor.ViewModels;
using ReactiveUI;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Nodus.ObjectDescriptor.Controls;

public partial class ExposedCollectionEditor : ReactiveUserControl<ExposedCollectionViewModel>
{
    public ExposedCollectionEditor()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.Label, v => v.NameText.Text)
                .DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.CurrentValue, v => v.ExpandButton.IsVisible,
                    x => x != null)
                .DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.IsExpanded, v => v.Items.IsVisible)
                .DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.IsExpanded, v => v.ExpandIcon.Kind,
                    x => x ? MaterialIconKind.ChevronUp : MaterialIconKind.ChevronDown)
                .DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.Items, v => v.Items.ItemsSource)
                .DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.CurrentValue, v => v.CreateButton.IsVisible,
                    x => x == null && (ViewModel?.CanCreateNewInstances ?? true))
                .DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.CurrentValue, v => v.AddNewButton.IsVisible,
                    x => x != null && (ViewModel?.CanCreateNewItems ?? true))
                .DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.Items.Count, v => v.CountText.Text)
                .DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.CurrentValue, v => v.CountText.IsVisible,
                    x => x != null)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.Reset, v => v.ResetButton)
                .DisposeWith(d);
            this.BindCommand(ViewModel, vm => vm.AddNewItem, v => v.AddNewButton)
                .DisposeWith(d);
            this.BindCommand(ViewModel, vm => vm.Expand, v => v.ExpandButton)
                .DisposeWith(d);
            this.BindCommand(ViewModel, vm => vm.TryCreateNew, v => v.CreateButton)
                .DisposeWith(d);
        });
    }
}