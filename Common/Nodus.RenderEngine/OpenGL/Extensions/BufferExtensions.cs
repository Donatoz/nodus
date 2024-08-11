using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

public static class BufferExtensions
{
    public static void ConformToStandardVertex(this IGlVertexArray vao)
    {
        vao.SetVertexAttribute(0, 3, VertexAttribPointerType.Float, 8, 0);
        vao.SetVertexAttribute(1, 3, VertexAttribPointerType.Float, 8, 3);
        vao.SetVertexAttribute(2, 2, VertexAttribPointerType.Float, 8, 6);
    }
}