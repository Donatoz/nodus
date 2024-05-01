using System.Reactive.Disposables;
using Nodus.Core.Reactive;
using Nodus.NodeEditor.Models;
using ReactiveUI;

namespace Nodus.NodeEditor.ViewModels;

public class CommentViewModel : ElementViewModel
{
    public MutableReactiveProperty<string> Content { get; }

    private readonly CompositeDisposable disposables;
    
    public CommentViewModel(ICommentModel model) : base(model)
    {
        disposables = new CompositeDisposable();
        
        Content = new MutableReactiveProperty<string>(model.Content.Value)
            .DisposeWith(disposables);
        
        Content.AlterationStream
            .BindTo(model, x => x.Content.Value)
            .DisposeWith(disposables);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        if (!disposing) return;
        
        disposables.Dispose();
    }
}