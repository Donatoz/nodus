using System.Diagnostics;
using System.Windows.Input;
using Nodus.Core.Common;
using Nodus.Core.Entities;
using Nodus.Core.Selection;
using Nodus.Core.ViewModels;
using Nodus.DI.Factories;
using Nodus.NodeEditor.Meta;
using Nodus.NodeEditor.Models;
using ReactiveUI;

namespace Nodus.NodeEditor.ViewModels;

public class ElementViewModel : ReactiveViewModel, ISelectable
{
    public string ElementId { get; }
    public VisualGraphElementData? VisualData { get; protected set; }

    public ICommand DeleteSelf { get; }
    
    public ElementViewModel(IGraphElementModel model)
    {
        ElementId = model.ElementId;
        DeleteSelf = ReactiveCommand.Create(OnDeleteSelf);
        
        if (model is IEntity e && e.TryGetGeneric(out IContainer<IGraphElementData> data))
        {
            VisualData = data.Value.VisualData;
        }
    }
    
    private void OnDeleteSelf()
    {
        RaiseEvent(new ElementDeleteRequest(this));
    }

    public virtual void Select()
    {
        RaiseEvent(new SelectionEvent(this, true));
    }

    public virtual void Deselect()
    {
        RaiseEvent(new SelectionEvent(this, false));
    }
}

public readonly struct ElementDeleteRequest : IEvent
{
    public ElementViewModel Element { get; }

    public ElementDeleteRequest(ElementViewModel element)
    {
        Element = element;
    }
}