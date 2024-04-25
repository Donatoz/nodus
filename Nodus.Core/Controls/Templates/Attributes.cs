using System;

namespace Nodus.Core.Controls.Templates;

/// <summary>
/// Specifies the determinative data template.
/// This type will be handled as a IDataTemplate which is bound to the specified ContextType and OVERRIDE
/// any existing data template bindings within specific template selector context.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class DataTemplateProviderAttribute : Attribute
{
    public Type ContextType { get; }
    public bool Override { get; }

    public DataTemplateProviderAttribute(Type contextType, bool overrideOther = false)
    {
        ContextType = contextType;
        Override = overrideOther;
    }
}