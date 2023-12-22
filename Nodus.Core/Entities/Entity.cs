using System;

namespace Nodus.Core.Entities;

public abstract class Entity : IEntity, IDisposable
{
    public abstract string EntityId { get; }

    protected Entity()
    {
        this.Register();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.Forget();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}