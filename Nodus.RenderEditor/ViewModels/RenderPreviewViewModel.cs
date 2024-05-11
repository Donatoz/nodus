using System.Reactive;
using Nodus.Core.Common;
using Nodus.Core.Reactive;
using Nodus.Core.ViewModels;
using ReactiveUI;

namespace Nodus.RenderEditor.ViewModels;

public class RenderPreviewViewModel : ReactiveViewModel
{
    public MutableReactiveProperty<bool> Render { get; }
    
    public ReactiveCommand<Unit, Unit> UpdatePreview { get; }

    public RenderPreviewViewModel()
    {
        Render = new MutableReactiveProperty<bool>(true);
        
        UpdatePreview = ReactiveCommand.Create(UpdatePreviewImpl);
    }

    private void UpdatePreviewImpl()
    {
        RaiseEvent(new UpdatePreviewEvent());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        if (!disposing) return;
        
        Render.Dispose();
    }

    public readonly struct UpdatePreviewEvent : IEvent { }
}