using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Nodus.Core.Extensions;

namespace Nodus.Core.Controls.Templates;

public abstract class ReflectionTemplateSelector<TCtx> : IDataTemplate
{
    private readonly IDictionary<Type, Func<TCtx, Control?>> factories;

    protected ReflectionTemplateSelector()
    {
        factories = new Dictionary<Type, Func<TCtx, Control?>>();
        Repopulate();
    }

    public void Repopulate()
    {
        factories.Clear();
        PopulateCache(factories);
    }

    protected abstract void PopulateCache(IDictionary<Type, Func<TCtx, Control?>> cache);
    
    public Control? Build(object? param)
    {
        return param != null ? factories[param.GetType()].Invoke(param.MustBe<TCtx>()) : null;
    }

    public bool Match(object? data)
    {
        return data is TCtx && factories.ContainsKey(data.GetType());
    }

    protected virtual Control CreateControlWithContext(Type controlType, TCtx context)
    {
        var c = Activator.CreateInstance(controlType).NotNull().MustBe<Control>();
        c.DataContext = context;
        return c;
    }
}