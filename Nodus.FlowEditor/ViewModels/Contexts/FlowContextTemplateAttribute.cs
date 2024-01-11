using System;

namespace FlowEditor.ViewModels.Contexts;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class FlowContextTemplateAttribute : Attribute
{
    public Type ControlType { get; }

    public FlowContextTemplateAttribute(Type controlType)
    {
        ControlType = controlType;
    }
}