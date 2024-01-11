using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Controls;
using Nodus.Core.Controls.Templates;
using Nodus.Core.Extensions;
using Nodus.NodeEditor.ViewModels;

namespace FlowEditor.ViewModels.Contexts;

[DataTemplateProvider(typeof(INodeContextViewModel))]
public class FlowContextTemplateSelector : ReflectionTemplateSelector<IFlowContextViewModel>
{
    protected override void PopulateCache(IDictionary<Type, Func<IFlowContextViewModel, Control?>> cache)
    {
        AppDomain.CurrentDomain.ForEachAsmTypeWithAttribute<FlowContextTemplateAttribute>(x =>
        {
            var attr = x.GetCustomAttribute<FlowContextTemplateAttribute>()!;
            cache[x] = ctx => CreateControlWithContext(attr.ControlType, ctx);
        });
    }
}