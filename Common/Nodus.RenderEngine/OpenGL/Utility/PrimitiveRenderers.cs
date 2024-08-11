using System.Numerics;
using Nodus.RenderEngine.Common;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

public delegate IRenderer GlRendererFactory();

public static class GeometryStructures
{
    public static class Quad
    {
        public static readonly Vertex[] Vertices =
        {
            new(new Vector3(1,  1f, 0.0f), Vector3.Zero, new Vector2(1, 1)), // Top right
            new(new Vector3(1f, -1f, 0.0f), Vector3.Zero, new Vector2(1, 0)), // Bottom right
            new(new Vector3(-1, -1f, 0.0f), Vector3.Zero, new Vector2(0, 0)), // Bottom left
            new(new Vector3(-1f,  1f, 0.0f), Vector3.Zero, new Vector2(0, 1)) // Top left
        };
        
        public static readonly uint[] Indices =
        {
            0, 1, 3,
            1, 2, 3
        };
    }

    public static class Triangle
    {
        public static readonly Vertex[] Vertices =
        {
            new(new Vector3(0, 1f, 0.0f), Vector3.Zero, new Vector2(0.5f, 1)), // Top
            new(new Vector3(1f, -1f, 0.0f), Vector3.Zero, new Vector2(1, 0)), // Bottom right
            new(new Vector3(-1, -1f, 0.0f), Vector3.Zero, new Vector2(0, 0)), // Bottom left
        };

        public static readonly uint[] Indices =
        {
            0, 1, 2
        };
    }
}

public static class PrimitiveRenderers
{
    public static readonly GlRendererFactory QuadRenderer = () => new GlGeometryPrimitiveRenderer(
        new GeometryPrimitive(GeometryStructures.Quad.Vertices, GeometryStructures.Quad.Indices), new Transform2D());
    
    public static readonly GlRendererFactory TriangleRenderer = () => new GlGeometryPrimitiveRenderer(
        new GeometryPrimitive(GeometryStructures.Triangle.Vertices, GeometryStructures.Triangle.Indices), new Transform2D());
}