using System.Reactive.Disposables;
using Avalonia;
using Avalonia.ReactiveUI;
using Material.Icons;
using Nodus.Core.Extensions;
using Nodus.ObjectDescriptor.ViewModels;
using ReactiveUI;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Nodus.ObjectDescriptor.Controls;

public partial class ExposedCompoundEditor : ReactiveUserControl<ExposedCompoundViewModel>
{
    public static readonly AvaloniaProperty<bool> IsChildProperty;
    
    public bool IsChild
    {
        get => (bool) GetValue(IsChildProperty).NotNull();
        set => SetValue(IsChildProperty, value);
    }

    static ExposedCompoundEditor()
    {
        IsChildProperty = AvaloniaProperty.Register<ExposedCompoundEditor, bool>(nameof(IsChildProperty));
    }
    
    public ExposedCompoundEditor()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.Label, v => v.NameText.Text)
                .DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.IsExpanded, v => v.MembersList.IsVisible)
                .DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.IsExpanded, v => v.ExpandIcon.Kind,
                    x => x ? MaterialIconKind.ChevronUp : MaterialIconKind.ChevronDown)
                .DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.CurrentValue, v => v.ExpandButton.IsVisible,
                    x => x != null)
                .DisposeWith(d);
            this.OneWayBind(ViewModel, vm => vm.CurrentValue, v => v.CreateButton.IsVisible,
                    x => x == null && (ViewModel?.CanCreateNewInstances ?? true))
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.Reset, v => v.ResetButton)
                .DisposeWith(d);
            this.BindCommand(ViewModel, vm => vm.Expand, v => v.ExpandButton)
                .DisposeWith(d);
            this.BindCommand(ViewModel, vm => vm.TryCreateNew, v => v.CreateButton)
                .DisposeWith(d);

            MembersList.DataContext = ViewModel?.DescribedExposedValue;

            if (ViewModel != null)
            {
                TypeIcon.Classes.Clear();
                TypeIcon.Classes.Add("type-icon");
                TypeIcon.Classes.Add(GetTypeIconClass(ViewModel.ValueType));
            }
        });
    }

    protected virtual string GetTypeIconClass(Type type)
    {
        if (type.IsClass) return "class";
        if (type.IsValueType) return "struct";

        return string.Empty;
    }
}