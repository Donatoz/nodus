using Nodus.RenderEngine.Common;

namespace Nodus.RenderEngine.OpenGL;

/// <summary>
/// Represents a definition for an OpenGL texture.
/// </summary>
public interface IGlTextureDefinition
{
    /// <summary>
    /// A texture source.
    /// </summary>
    ITextureSource Source { get; }

    /// <summary>
    /// The specification for an OpenGL texture.
    /// </summary>
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