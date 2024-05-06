namespace Nodus.RenderEngine.Common;

public interface IGeometryPrimitive
{
    Vertex[] Vertices { get; }
    uint[] Indices { get; }
}

public readonly struct GeometryPrimitive : IGeometryPrimitive
{
    public Vertex[] Vertices { get; }
    public uint[] Indices { get; }

    public GeometryPrimitive(Vertex[] vertices, uint[] indices)
    {
        Vertices = vertices;
        Indices = indices;
    }
}