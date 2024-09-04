using Nodus.Core.Extensions;
using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.OpenGL.Convention;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

public interface IGlPrimitiveRenderContext : IGlRenderContext
{
    IReadOnlyCollection<IGlShaderUniform> Uniforms { get; }
    IReadOnlyCollection<IGlTextureDefinition> Textures { get; }
}

public record GlPrimitiveContext(
    IReadOnlyCollection<IGlShaderUniform> Uniforms,
    IReadOnlyCollection<IGlTextureDefinition> Textures)
    : GlContext, IGlPrimitiveRenderContext;

/// <summary>
/// An OpenGL context renderer that renders a geometry primitives using provided shaders, uniforms and textures.
/// </summary>
public class GlGeometryPrimitiveRenderer : GlRendererBase, IDisposable
{
    private IGlVertexArray? vao;
    private IGlBuffer<Vertex>? vbo;
    private IGlBuffer<uint>? ebo;
    private GlShaderProgram? program;
    private GL? gl;
    private IEnumerable<IGlShaderUniform>? uniforms;
    private IEnumerable<IGlTexture>? textures;
    
    private IGeometryPrimitive primitive;
    private readonly ITransform transform;
    private GlMatrix4Uniform primitiveTransformUniform;
    
    public GlGeometryPrimitiveRenderer(IGeometryPrimitive primitive, ITransform transform)
    {
        this.primitive = primitive;
        this.transform = transform;
        primitiveTransformUniform = new GlMatrix4Uniform("transform", this.transform.GetMatrix, true);
    }
    
    public override void Initialize(IRenderContext context, IRenderBackendProvider backendProvider)
    {
        Enqueue(() => InitializeImpl(context, backendProvider));
    }

    private void InitializeImpl(IRenderContext context, IRenderBackendProvider backendProvider)
    {
        TryDisposeBuffers();
        textures?.DisposeAll();

        var primitiveContext = context.MustBe<IGlPrimitiveRenderContext>();
        
        gl = backendProvider.GetBackend<GL>();
        uniforms = primitiveContext.Uniforms;

        vao = new GlVertexArray<float>(gl);
        vao.Bind();

        vbo = new GlBuffer<Vertex>(gl, primitive.Vertices, BufferTargetARB.ArrayBuffer, false);
        ebo = new GlBuffer<uint>(gl, primitive.Indices, BufferTargetARB.ElementArrayBuffer, false);
        
        vao.ConformToStandardVertex();

        textures = primitiveContext.Textures.Select(x => new GlTexture(gl, x.Source, x.Specification, this)).ToArray();
        
        gl.TryThrowAllErrors();
        gl.BindVertexArray(0);
    }

    public void UpdatePrimitive(IGeometryPrimitive newPrimitive)
    {
        Enqueue(() => UpdatePrimitiveImpl(newPrimitive));
    }

    private void UpdatePrimitiveImpl(IGeometryPrimitive newPrimitive)
    {
        ValidateRenderState();
        
        primitive = newPrimitive;
        primitiveTransformUniform = new GlMatrix4Uniform(UniformConvention.TransformUniformName, transform.GetMatrix, true);

        vao!.Bind();
        vbo!.Bind();
        ebo!.Bind();
        
        vbo.UpdateData(newPrimitive.Vertices);
        ebo.UpdateData(primitive.Indices);
        
        gl!.BindVertexArray(0);
    }

    public override unsafe void RenderFrame()
    {
        base.RenderFrame();
        
        ValidateRenderState();
        
        gl!.TryThrowNextError();

        UpdateUniforms();

        gl!.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
        
        program?.Use();
        textures?.ForEach(x => x.TryBind());
        vao!.Bind();
        
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
        
        program.ApplyUniform(primitiveTransformUniform);
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

    public override void UpdateShaders(IEnumerable<IShaderDefinition> shaders)
    {
        Enqueue(() => UpdateProgram(shaders));
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