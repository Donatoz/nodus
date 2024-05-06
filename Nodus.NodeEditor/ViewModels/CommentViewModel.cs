using System.Reactive.Disposables;
using System.Windows.Input;
using Nodus.Core.Reactive;
using Nodus.NodeEditor.Models;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive.Linq;

namespace Nodus.NodeEditor.ViewModels;

public class CommentViewModel : ElementViewModel
{
    public MutableReactiveProperty<string> Content { get; }
    public IReactiveProperty<bool> IsInEditMode => isInEditMode;
    
    public ICommand SwitchEditMode { get; }

    private readonly CompositeDisposable disposables;
    private readonly MutableReactiveProperty<bool> isInEditMode;
    
    public CommentViewModel(ICommentModel model) : base(model)
    {
        disposables = new CompositeDisposable();
        
        isInEditMode = new MutableReactiveProperty<bool>()
            .DisposeWith(disposables);
        Content = new MutableReactiveProperty<string>(model.Content.Value)
            .DisposeWith(disposables);
        
        Content.AlterationStream
            .Select(x => string.IsNullOrWhiteSpace(x) ? "New Comment" : x)
            .Do(x => Trace.WriteLine(x))
            .Subscribe(model.SetContent)
            .DisposeWith(disposables);

        SwitchEditMode = ReactiveCommand.Create(OnSwitchEdit);
    }

    private void OnSwitchEdit()
    {
        isInEditMode.SetValue(!isInEditMode.Value);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        if (!disposing) return;
        
        disposables.Dispose();
    }
}