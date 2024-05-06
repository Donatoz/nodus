using System.Numerics;
using Nodus.RenderEngine.Common;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

public delegate IRenderer<GL> GlRendererFactory();

public static class PrimitiveRenderers
{
    private static readonly Vertex[] QuadVertices =
    {
        new(new Vector3(1,  1f, 0.0f), new Vector4(1, 0, 0, 1)), // Top right
        new(new Vector3(1f, -1f, 0.0f), new Vector4(0, 0, 0, 1)), // Bottom right
        new(new Vector3(-1, -1f, 0.0f), new Vector4(0, 0, 1, 1)), // Bottom left
        new(new Vector3(-1f,  1f, 0.0f), new Vector4(0, 0, 0, 1)) // Top left
    };

    private static readonly uint[] QuadIndices =
    {
        0, 1, 3,
        1, 2, 3
    };
    
    public static readonly GlRendererFactory QuadRenderer = () => new GlGeometryPrimitiveRenderer(new GeometryPrimitive(QuadVertices, QuadIndices));
}