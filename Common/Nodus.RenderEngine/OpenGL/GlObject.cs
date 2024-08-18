using Nodus.RenderEngine.Common;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

/// <summary>
/// Represents an OpenGL object.
/// </summary>
public class GlObject : TrackedRenderContextObject<uint, GL>
{
    protected IRenderTracer? Tracer { get; }
    
    public GlObject(GL context, IRenderTracer? tracer = null) : base(context)
    {
        Tracer = tracer;
    }

    protected void TryThrowTracedGlError(string frameLabel, string message)
    {
#if DEBUG
        Tracer?.PutFrame(new RenderTraceFrame(frameLabel));
        Context.TryThrowNextError(message, Tracer);
        Tracer?.TryWithdrawFrame();
#else
        Context.TryThrowNextError(message);
#endif
    }
}