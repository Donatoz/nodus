namespace Nodus.RenderEngine.Common;

public abstract class RenderContextObject<THandle, TCtx> where THandle : unmanaged
{
    public THandle Handle { get; protected set; }
    
    protected TCtx Context { get; private set; }

    protected RenderContextObject(TCtx context)
    {
        Context = context;
    }

    public virtual void UpdateContext(TCtx context)
    {
        Context = context;
    }
}