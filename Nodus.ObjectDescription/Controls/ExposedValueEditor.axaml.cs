using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Nodus.ObjectDescriptor.ViewModels;
using ReactiveUI;

namespace Nodus.ObjectDescriptor.Controls;

public partial class ExposedValueEditor : ReactiveUserControl<ExposedValueViewModel>
{
    protected Panel RightExtensionsPanel => RightExtensions;

    protected virtual string TypeIndicatorPrefixClass => "type-indicator";
    
    public ExposedValueEditor()
    {
        InitializeComponent();

        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, vm => vm.Label, v => v.NameText.Text)
                .DisposeWith(d);

            this.BindCommand(ViewModel, vm => vm.Reset, v => v.ResetButton)
                .DisposeWith(d);
            
            TypeIndicator.Classes.Clear();
            TypeIndicator.Classes.Add(TypeIndicatorPrefixClass);
            TypeIndicator.Classes.Add(ViewModel?.ValueType.Name ?? "Null");
        });
    }
}