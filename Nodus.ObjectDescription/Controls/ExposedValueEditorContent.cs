using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using Nodus.ObjectDescriptor.ViewModels;
using ReactiveUI;

namespace Nodus.ObjectDescriptor.Controls;

public abstract class ExposedValueEditorContent : ReactiveUserControl<ExposedValueViewModel>
{
    public ExposedValueEditorContent()
    {
        this.WhenActivated(d =>
        {
            ViewModel.WhenAnyValue(x => x.CurrentValue)
                .Subscribe(OnValueExplicitlyChanged)
                .DisposeWith(d);
        });
    }

    protected virtual void OnValueExplicitlyChanged(object? value) { }
}