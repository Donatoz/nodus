using System;
using System.Collections.Generic;
using FlowEditor.Models;
using FlowEditor.ViewModels.Contexts;
using Nodus.DI.Factories;

namespace FlowEditor.Factories;

public class FlowContextViewModelFactory : IFactory<IFlowContext, IFlowContextViewModel>
{
    protected delegate IFlowContextViewModel SubFactory(IFlowContext viewModel);
    
    public static FlowContextViewModelFactory Default { get; } = new();

    private readonly Dictionary<Type, SubFactory> subFactories;
    
    public FlowContextViewModelFactory()
    {
        subFactories = new Dictionary<Type, SubFactory>();
        PopulateCache(subFactories);
    }

    protected virtual void PopulateCache(IDictionary<Type, SubFactory> cache)
    {
        
    }
    
    public IFlowContextViewModel Create(IFlowContext ctx)
    {
        return subFactories.ContainsKey(ctx.GetType())
            ? subFactories[ctx.GetType()].Invoke(ctx)
            : new GenericFlowContextViewModel(ctx);
    }
}