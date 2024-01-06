using System.Diagnostics;
using System.Reflection;

namespace Nodus.Core.ObjectDescription;

public interface IPropertyBinding
{
    void SetValue(object? value);
}

public readonly struct ReflectionPropertyBinding : IPropertyBinding
{
    private readonly PropertyInfo info;
    private readonly object target;
    
    public ReflectionPropertyBinding(PropertyInfo info, object target)
    {
        this.info = info;
        this.target = target;
    }
    
    public void SetValue(object? value)
    {
        Trace.WriteLine($"------------ Set value: {value}");
        info.SetValue(target, value);
    }
}