using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Nodus.Core.Extensions;

namespace Nodus.Core.Controls.Templates;

public sealed class CumulativeTemplateSelector : IDataTemplate
{
    private readonly ISet<TemplateContainer> templates;
    
    public CumulativeTemplateSelector()
    {
        templates = new HashSet<TemplateContainer>();
        
        Repopulate();
    }

    private void Repopulate()
    {
        AppDomain.CurrentDomain.GetAssemblies().ForEach(asm =>
        {
            asm.GetTypes().Where(t => t.IsDefined(typeof(DataTemplateProviderAttribute), false)).ForEach(t =>
            {
                var attr = t.GetCustomAttribute<DataTemplateProviderAttribute>()!;
                var o = Activator.CreateInstance(t);
                if (o is not IDataTemplate dt) return;
                
                templates.Add(new TemplateContainer(attr.ContextType, dt));
            });
        });
    }
    
    public Control? Build(object? param)
    {
        return param == null ? null : templates.First(t => t.ContextType.IsInstanceOfType(param)).DataTemplate.Build(param);
    }

    public bool Match(object? data)
    {
        return data != null && templates.Any(d => d.ContextType.IsInstanceOfType(data));
    }

    public readonly struct TemplateContainer
    {
        public Type ContextType { get; }
        public IDataTemplate DataTemplate { get; }

        public TemplateContainer(Type contextType, IDataTemplate dataTemplate)
        {
            ContextType = contextType;
            DataTemplate = dataTemplate;
        }
    }
}