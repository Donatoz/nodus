namespace Nodus.RenderEngine.Common;

public abstract class RenderContextObject<TCtx>
{
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

public abstract class TrackedRenderContextObject<THandle, TCtx> : RenderContextObject<TCtx> where THandle : unmanaged
{
    public THandle Handle { get; protected set; }
    
    protected TrackedRenderContextObject(TCtx context) : base(context)
    {
    }
}