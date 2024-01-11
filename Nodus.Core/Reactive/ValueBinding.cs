using System;

namespace Nodus.Core.Reactive;

public readonly struct ValueBinding
{
    public Func<object?> Getter { get; }
    public Action<object?> Setter { get; }

    public ValueBinding(Func<object?> getter, Action<object?> setter)
    {
        Getter = getter;
        Setter = setter;
    }
}