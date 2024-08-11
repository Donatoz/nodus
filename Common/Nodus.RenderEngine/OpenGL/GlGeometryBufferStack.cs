using Nodus.RenderEngine.Common;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

/// <summary>
/// Represents a set of OpenGL buffers used to render geometry primitives using indexed drawing.
/// </summary>
public class GlGeometryBufferStack : IDisposable
{
    private readonly IGlVertexArray vao;
    private readonly IGlBuffer<Vertex> vbo;
    private readonly IGlBuffer<uint> ebo;
    private readonly GL gl;

    public GlGeometryBufferStack(GL gl)
    {
        this.gl = gl;
        vao = new GlVertexArray<float>(gl);
        vao.Bind();
        
        vbo = new GlBuffer<Vertex>(gl, new Span<Vertex>(), BufferTargetARB.ArrayBuffer, false);
        ebo = new GlBuffer<uint>(gl, new Span<uint>(), BufferTargetARB.ElementArrayBuffer, false);
        
        vao.ConformToStandardVertex();
        
        gl.BindVertexArray(0);
    }

    public void Bind()
    {
        vao.Bind();
    }
    
    public void UpdateGeometry(IGeometryPrimitive primitive)
    {
        vao.Bind();
        vbo.Bind();
        ebo.Bind();
        
        vbo.UpdateData(primitive.Vertices);
        ebo.UpdateData(primitive.Indices);
        
        gl.BindVertexArray(0);
    }

    public void Dispose()
    {
        vao.Dispose();
        vbo.Dispose();
        ebo.Dispose();
    }
}