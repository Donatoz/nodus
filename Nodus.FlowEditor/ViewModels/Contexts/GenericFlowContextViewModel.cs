using System.Collections.Generic;
using System.Linq;
using FlowEditor.Models;
using FlowEditor.Models.Primitives;
using FlowEditor.Views.Editors;
using Nodus.Core.ObjectDescription;
using Nodus.Core.Reactive;

namespace FlowEditor.ViewModels.Contexts;

[FlowContextTemplate(typeof(GenericFlowContextEditor))]
public sealed class GenericFlowContextViewModel : IFlowContextViewModel
{
    private readonly MutableReactiveProperty<DescriptionProvider?> context;

    public IReactiveProperty<DescriptionProvider?> DescribedContext => context;
    public bool IsValid => DescribedContext.Value != null;

    public GenericFlowContextViewModel(IFlowContext? initialContext = null)
    {
        context = new MutableReactiveProperty<DescriptionProvider?>();

        if (initialContext != null)
        {
            ChangeModel(initialContext);
        }
    }

    public void ChangeModel(IFlowContext context)
    {
        this.context.SetValue(context.GetDescriptionProvider());
    }

    public void Dispose()
    {
        context.Dispose();
    }
}