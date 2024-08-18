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

    private readonly IRenderTracer? tracer;

    public GlGeometryBufferStack(GL gl, IRenderTracer? tracer = null)
    {
        this.gl = gl;
        this.tracer = tracer;
        vao = new GlVertexArray<float>(gl, tracer);
        vao.Bind();
        
        vbo = new GlBuffer<Vertex>(gl, new Span<Vertex>(), BufferTargetARB.ArrayBuffer, false, tracer);
        ebo = new GlBuffer<uint>(gl, new Span<uint>(), BufferTargetARB.ElementArrayBuffer, false, tracer);
        
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