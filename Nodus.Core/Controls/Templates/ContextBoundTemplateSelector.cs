using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Nodus.Core.Extensions;

namespace Nodus.Core.Controls.Templates;

/// <summary>
/// Represents a template selector that selects a data template based on the context type.
/// </summary>
public sealed class ContextBoundTemplateSelector : IDataTemplate
{
    private readonly IDictionary<Type, IList<IDataTemplate>> templates;

    public ContextBoundTemplateSelector()
    {
        templates = new Dictionary<Type, IList<IDataTemplate>>();

        Repopulate();
    }

    private void Repopulate()
    {
        AppDomain.CurrentDomain.ForEachAsmTypeWithAttribute<DataTemplateProviderAttribute>(t =>
        {
            var attr = t.GetCustomAttribute<DataTemplateProviderAttribute>()!;

            if (Activator.CreateInstance(t) is not IDataTemplate dt) return;

            IncludeDataTemplate(dt, attr.ContextType, attr.Override);
        });
    }

    private void IncludeDataTemplate(IDataTemplate template, Type contextType, bool supplement)
    {
        if (!templates.ContainsKey(contextType))
        {
            templates[contextType] = new List<IDataTemplate>();
        }

        if (!supplement)
        {
            templates[contextType].Clear();
        }

        templates[contextType].Add(template);
    }

    public Control? Build(object? param)
    {
        return param == null
            ? null
            : templates[templates.Keys.First(t => t.IsInstanceOfType(param))].FirstOrDefault(x => x.Match(param))?.Build(param);
    }

    public bool Match(object? data)
    {
        return data != null && templates.Keys.Any(t => t.IsInstanceOfType(data));
    }
}