using Nodus.RenderEngine.Common;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

public interface IGlRenderContext : IRenderContext
{
}

public record GlContext(IEnumerable<IShaderDefinition> CoreShaders) : IGlRenderContext;