using Nodus.RenderEngine.Common;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

public interface IGlRenderContext : IRenderContext
{
    GL GraphicsContext { get; }
}

public record GlContext(GL GraphicsContext) : IGlRenderContext;