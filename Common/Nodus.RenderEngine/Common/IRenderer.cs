using Nodus.NodeEditor.Models;

namespace Nodus.RenderEngine.Common;

public interface IRenderContext
{
    IEnumerable<IShaderDefinition> CoreShaders { get; }
}

/// <summary>
/// Represents a renderer that is responsible for rendering frames using a specified rendering backend.
/// </summary>
public interface IRenderer : IRenderDispatcher
{
    /// <summary>
    /// Initializes the renderer with the specified context and backend provider.
    /// </summary>
    /// <param name="context">The render context to use for initialization.</param>
    /// <param name="backendProvider">The backend provider to use for initialization.</param>
    void Initialize(IRenderContext context, IRenderBackendProvider backendProvider);

    /// <summary>
    /// Renders a frame.
    /// </summary>
    void RenderFrame();

    /// <summary>
    /// Updates the shaders used by the renderer.
    /// </summary>
    /// <param name="shaders">The collection of shader definitions to update.</param>
    void UpdateShaders(IEnumerable<IShaderDefinition> shaders);
}