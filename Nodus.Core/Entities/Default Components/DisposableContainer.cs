using System;

namespace Nodus.Core.Entities;

public record DisposableContainer<T>(T Value) : IContainer<T>, IDisposable where T : class, IDisposable
{
    public void Dispose()
    {
        Value.Dispose();
    }
}