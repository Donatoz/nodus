using Nodus.RenderEngine.Common;

namespace Nodus.RenderEngine.OpenGL;

public interface IGlTextureDefinition
{
    ITextureSource Source { get; }
    IGlTextureSpecification Specification { get; }
}

public readonly struct GlTextureDefinition : IGlTextureDefinition
{
    public ITextureSource Source { get; }
    public IGlTextureSpecification Specification { get; }

    public GlTextureDefinition(ITextureSource source, IGlTextureSpecification specification)
    {
        Source = source;
        Specification = specification;
    }
}