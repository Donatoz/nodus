using System.Collections.Generic;
using System.Linq;
using FlowEditor.Models;
using FlowEditor.Views.Editors;
using Nodus.Core.ObjectDescription;
using Nodus.Core.Reactive;

namespace FlowEditor.ViewModels.Contexts;

[FlowContextTemplate(typeof(GenericFlowContextEditor))]
public sealed class GenericFlowContextViewModel : IFlowContextViewModel
{
    private readonly MutableReactiveProperty<IEnumerable<PropertyEditorViewModel>> editors;

    public IReactiveProperty<IEnumerable<PropertyEditorViewModel>> Editors => editors;
    public bool IsValid => Editors.Value.Any();

    public GenericFlowContextViewModel(IFlowContext? initialContext = null)
    {
        editors = new MutableReactiveProperty<IEnumerable<PropertyEditorViewModel>>(Enumerable.Empty<PropertyEditorViewModel>());

        if (initialContext != null)
        {
            ChangeModel(initialContext);
        }
    }

    public void ChangeModel(IFlowContext context)
    {
        var props = context.Mutator?
                        .GetProperties()
                        .Select(x =>
                            new PropertyEditorViewModel(x.PropertyName, 
                                x.PropertyType,
                                x.Description ?? string.Empty, 
                                new DirectPropertyBinding(x.PropertyBinding.Setter, x.PropertyBinding.Getter))) 
                    ?? Enumerable.Empty<PropertyEditorViewModel>();
        
        editors.SetValue(props);
    }


    public void Dispose()
    {
        editors.Dispose();
    }
}