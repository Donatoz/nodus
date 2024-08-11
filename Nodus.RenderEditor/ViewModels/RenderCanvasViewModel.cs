using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Nodus.Core.Entities;
using Nodus.Core.Extensions;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Models;
using Nodus.NodeEditor.ViewModels;
using Nodus.RenderEditor.Factories;
using Nodus.RenderEditor.Models;
using Nodus.RenderEditor.Models.Contexts;
using IRenderContext = Nodus.RenderEngine.Common.IRenderContext;

namespace Nodus.RenderEditor.ViewModels;

public class RenderCanvasViewModel : NodeCanvasViewModel
{
    public IObservable<IRenderContext> ContextStream => contextSubject;
    
    private readonly Subject<IRenderContext> contextSubject;

    public RenderCanvasViewModel(INodeCanvasModel model, 
        IServiceProvider serviceProvider, 
        IRenderCanvasViewModelComponentFactory componentFactory, 
        IFactory<IGraphElementModel, ElementViewModel> elementsFactory) : base(model, serviceProvider, componentFactory, elementsFactory)
    {
        contextSubject = new Subject<IRenderContext>();
        
        this.AttachContainer(componentFactory.CreateContentBrowser());
        
        var renderPreview = componentFactory.CreateRenderPreview(ContextStream);
        
        renderPreview.AttachDisposable(renderPreview.EventStream.OfType<RenderPreviewViewModel.UpdatePreviewEvent>().Subscribe(OnUpdatePreview));
        this.AttachDisposableContainer(renderPreview);
    }

    private void OnUpdatePreview(RenderPreviewViewModel.UpdatePreviewEvent evt)
    {
        var master = Model.Elements.OfType<IRenderNodeModel>()
            .FirstOrDefault(x => x.Context.Value is IRenderMasterContext);
        
        if (master == null) return;

        var context = Model.MustBe<IRenderCanvasModel>().CreateContextFrom(master);
        
        contextSubject.OnNext(context);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        contextSubject.Dispose();
    }
}