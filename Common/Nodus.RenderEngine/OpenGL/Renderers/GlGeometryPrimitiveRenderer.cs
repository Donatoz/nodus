using System.Drawing;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Common;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

public interface IGlPrimitiveRenderContext : IGlRenderContext
{
    IEnumerable<IGlShaderUniform> Uniforms { get; }
    IEnumerable<IGlTextureDefinition> Textures { get; }
}

public record GlPrimitiveContext(
    GL GraphicsContext,
    IEnumerable<IGlShaderUniform> Uniforms,
    IEnumerable<IGlTextureDefinition> Textures)
    : GlContext(GraphicsContext), IGlPrimitiveRenderContext;

/// <summary>
/// A renderer that uses OpenGL to render a quad.
/// </summary>
public class GlGeometryPrimitiveRenderer : IRenderer, IDisposable
{
    private IGlVertexArray? vao;
    private IGlBuffer<Vertex>? vbo;
    private IGlBuffer<uint>? ebo;
    private GlShaderProgram? program;
    private GL? gl;
    private IEnumerable<IGlShaderUniform>? uniforms;
    private IEnumerable<IGlTexture>? textures;

    private readonly PriorityQueue<Action, RenderWorkPriority> renderWorkerQueue;
    private readonly IGeometryPrimitive primitive;
    
    public GlGeometryPrimitiveRenderer(IGeometryPrimitive primitive)
    {
        this.primitive = primitive;
        renderWorkerQueue = new PriorityQueue<Action, RenderWorkPriority>();
    }
    
    public void Initialize(IRenderContext context)
    {
        TryDisposeBuffers();

        var primitiveContext = context.MustBe<IGlPrimitiveRenderContext>();
        
        gl = primitiveContext.GraphicsContext;
        uniforms = primitiveContext.Uniforms;
        
        gl.ClearColor(Color.Black);
        gl.Enable(EnableCap.DepthTest);
        //gl.Enable(EnableCap.Blend);
        //gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        vao = new GlVertexArray<float>(gl);
        vao.Bind();

        vbo = new GlBuffer<Vertex>(gl, primitive.Vertices, BufferTargetARB.ArrayBuffer, false);
        ebo = new GlBuffer<uint>(gl, primitive.Indices, BufferTargetARB.ElementArrayBuffer, false);
        
        vbo.Bind();
        ebo.Bind();
        
        vao.SetVertexAttribute(0, 3, VertexAttribPointerType.Float, 5, 0);
        vao.SetVertexAttribute(1, 2, VertexAttribPointerType.Float, 5, 3);

        textures = primitiveContext.Textures.Select(x => new GlTexture(gl, x.Source, x.Specification, this)).ToArray();
        
        gl.TryThrowAllErrors();
        gl.BindVertexArray(0);
    }

    public unsafe void RenderFrame()
    {
        ValidateRenderState();
        ExecuteRenderWorkerQueue();
        UpdateUniforms();
        
        gl!.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

        textures?.ForEach(x => x.TryBind());
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
    
    private void UpdateUniforms()
    {
        if (program == null || uniforms == null) return;
        
        uniforms.ForEach(program.ApplyUniform);
    }

    private void ExecuteRenderWorkerQueue()
    {
        while (renderWorkerQueue.Count > 0)
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

    public void Enqueue(Action item, RenderWorkPriority priority)
    {
        renderWorkerQueue.Enqueue(item, priority);
    }

    public void UpdateShaders(IEnumerable<IShaderDefinition> shaders)
    {
        Enqueue(() => UpdateProgram(shaders), RenderWorkPriority.Low);
    }

    public void Dispose()
    {
        TryDisposeBuffers();
        program?.Dispose();
        textures?.DisposeAll();
    }

    private void TryDisposeBuffers()
    {
        vao?.Dispose();
        vbo?.Dispose();
        ebo?.Dispose();
    }
}