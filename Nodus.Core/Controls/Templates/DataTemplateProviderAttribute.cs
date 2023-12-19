using System;

namespace Nodus.Core.Controls.Templates;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class DataTemplateProviderAttribute : Attribute
{
    public Type ContextType { get; }

    public DataTemplateProviderAttribute(Type contextType)
    {
        ContextType = contextType;
    }
}