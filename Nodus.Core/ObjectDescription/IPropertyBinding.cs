using System;
using System.Diagnostics;
using System.Reflection;
using Nodus.Core.Reactive;

namespace Nodus.Core.ObjectDescription;

public interface IPropertyBinding
{
    void SetValue(object? value);
    object? GetValue();
}

public readonly struct DirectPropertyBinding : IPropertyBinding
{
    private readonly Action<object?> setter;
    private readonly Func<object?> getter;
    
    public DirectPropertyBinding(Action<object?> setter, Func<object?> getter)
    {
        this.setter = setter;
        this.getter = getter;
    }

    public void SetValue(object? value)
    {
        setter.Invoke(value);
    }

    public object? GetValue()
    {
        return getter.Invoke();
    }
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
        info.SetValue(target, value);
    }

    public object? GetValue()
    {
        return info.GetValue(target);
    }
}