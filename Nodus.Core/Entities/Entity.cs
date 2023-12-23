using System;

namespace Nodus.Core.Entities;

/// <summary>
/// Represents a simple abstract base for entities.
/// </summary>
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