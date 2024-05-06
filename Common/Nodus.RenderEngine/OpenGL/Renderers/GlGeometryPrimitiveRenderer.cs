using System.Diagnostics;
using System.Drawing;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Common;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

public class GlGeometryPrimitiveRenderer : IRenderer<GL>, IDisposable
{
    private IGlVertexArray? vao;
    private IGlBuffer<Vertex>? vbo;
    private IGlBuffer<uint>? ebo;
    private GlShaderProgram? program;
    private GL? gl;
    
    private readonly Queue<Action> renderWorkerQueue;
    private readonly IGeometryPrimitive primitive;
    
    public GlGeometryPrimitiveRenderer(IGeometryPrimitive primitive)
    {
        this.primitive = primitive;
        renderWorkerQueue = new Queue<Action>();
    }
    
    public void Initialize(GL context)
    {
        TryDisposeBuffers();
        
        gl = context;
        
        gl.ClearColor(Color.Black);
        gl.Enable(EnableCap.DepthTest);

        vao = new GlVertexArray<float>(context);
        vbo = new GlBuffer<Vertex>(context, primitive.Vertices, BufferTargetARB.ArrayBuffer);
        ebo = new GlBuffer<uint>(context, primitive.Indices, BufferTargetARB.ElementArrayBuffer);
        
        vbo.Bind();
        ebo.Bind();
        
        vao.SetVertexAttribute(0, 3, VertexAttribPointerType.Float, 7, 0);
        vao.SetVertexAttribute(1, 4, VertexAttribPointerType.Float, 7, 3);
        
        gl.TryThrowNextError();
        gl.BindVertexArray(0);
    }

    public unsafe void RenderFrame()
    {
        ValidateRenderState();
        ExecuteRenderWorkerQueue();
        
        gl!.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
        
        vao!.Bind();
        program?.Use();

        gl.DrawElements(PrimitiveType.Triangles, (uint) primitive.Indices.Length, DrawElementsType.UnsignedInt, null);
    }

    private void UpdateProgram(IEnumerable<IShaderDefinition> definitions)
    {
        if (gl == null)
        {
            throw new Exception("Failed to update program: GL context is not set.");
        }
        
        program?.Dispose();

        var shaders = definitions.Select(x => new GlShader(gl, x.Source, x.Type.ToShaderType())).ToArray();

        program = new GlShaderProgram(gl, shaders);
        
        shaders.DisposeAll();
    }

    private void ExecuteRenderWorkerQueue()
    {
        while (renderWorkerQueue.Any())
        {
            renderWorkerQueue.Dequeue().Invoke();
        }
    }

    private void ValidateRenderState()
    {
        if (vbo == null || ebo == null || vao == null)
        {
            throw new Exception("Render state is invalid: buffers were not properly initialized.");
        }

        if (gl == null)
        {
            throw new Exception("Render state is invalid: GL context is not set.");
        }
    }

    public void Enqueue(Action item)
    {
        renderWorkerQueue.Enqueue(item);
    }

    public void UpdateShaders(IEnumerable<IShaderDefinition> shaders)
    {
        Enqueue(() => UpdateProgram(shaders));
    }

    public void Dispose()
    {
        TryDisposeBuffers();
        program?.Dispose();
    }

    private void TryDisposeBuffers()
    {
        vao?.Dispose();
        vbo?.Dispose();
        ebo?.Dispose();
    }
}