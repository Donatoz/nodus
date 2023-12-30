namespace Nodus.DI.Modules;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class ModuleInjectionEntryAttribute : Attribute
{
    public object ContextualBinding { get; }
    public int InjectionPriority { get; }

    public ModuleInjectionEntryAttribute(object context, int injectionPriority = 0)
    {
        ContextualBinding = context;
        InjectionPriority = injectionPriority;
    }
}