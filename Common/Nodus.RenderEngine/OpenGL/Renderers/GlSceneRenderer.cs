using System.Reactive.Disposables;
using Nodus.Core.Extensions;
using Nodus.RenderEngine.Common;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using PrimitiveType = Silk.NET.OpenGL.PrimitiveType;

namespace Nodus.RenderEngine.OpenGL;

public interface IGlSceneRenderContext : IRenderContext
{
    IRenderScene Scene { get; }
    IEnumerable<IMaterialDefinition> Materials { get; }
}

public record GlSceneRenderContext(
    IRenderScene Scene, 
    IEnumerable<IMaterialDefinition> Materials) 
    : IGlSceneRenderContext;

/// <summary>
/// An OpenGL context renderer that renders a scene using predefined materials and lighting.
/// </summary>
public class GlSceneRenderer : GlRendererBase, IDisposable
{
    private GlGeometryBufferStack? buffers;
    private GL? gl;
    private IGlSceneRenderContext? renderContext;
    private IGlMaterial? fallbackMaterial;
    private IGlDivBuffer? uniformBuffer;
    
    private readonly IGlMaterialDefinition fallbackMaterialDefinition;
    private readonly IDictionary<string, IGlMaterial> materials;
    private readonly IRenderTracer tracer;

    private readonly CompositeDisposable disposables;

    public GlSceneRenderer(IGlMaterialDefinition fallbackMaterial)
    {
        disposables = new CompositeDisposable();
        fallbackMaterialDefinition = fallbackMaterial;
        
        materials = new Dictionary<string, IGlMaterial>();
        tracer = new StackedRenderTracer();

        tracer.ExceptionStream.Subscribe(OnTraceException).DisposeWith(disposables);
    }

    #region Initialization
    
    public override void Initialize(IRenderContext context, IRenderBackendProvider backendProvider)
    {
        Enqueue(() => InitializeImpl(context, backendProvider));
    }

    private unsafe void InitializeImpl(IRenderContext context, IRenderBackendProvider backendProvider)
    {
        buffers?.Dispose();
        materials.Values.DisposeAll();
        materials.Clear();
        
        renderContext = context.MustBe<IGlSceneRenderContext>();

        gl = backendProvider.GetBackend<GL>();

        buffers = new GlGeometryBufferStack(gl, tracer);
        
        renderContext.Materials
            .OfType<IGlMaterialDefinition>()
            .ForEach(x => materials[x.MaterialId] = new GlMaterial(gl, x, tracer));

        fallbackMaterial ??= new GlMaterial(gl, fallbackMaterialDefinition, tracer);

        uniformBuffer = new GlDivBuffer(gl, BufferTargetARB.UniformBuffer, (nuint) (2 * sizeof(Matrix4X4<float>)), false, tracer);
        gl.BindBufferRange(GLEnum.UniformBuffer, 0, uniformBuffer.Handle, 0, (nuint) (2 * sizeof(Matrix4X4<float>)));
    }
    
    #endregion


    #region Render Loop
    
    public override void RenderFrame()
    {
        base.RenderFrame();
        ValidateRenderState();
        
        gl!.TryThrowNextError("Unannotated exception occured in the render loop.");

        gl!.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

        if (renderContext!.Scene.Viewer == null)
        {
            return;
        }
        
        UpdateBuffers();
        
        renderContext!.Scene.RenderedObjects
            .Where(x => x.IsRendered)
            .GroupBy(x => x.MaterialId)
            .Select(x => new
            {
                Group = x,
                Material = materials.TryGetValue(x.Key, out var material) ? material : fallbackMaterial!
            })
            .OrderBy(x => x.Material.RenderPriority)
            .ForEach(x => RenderMaterialGroup(x.Group, x.Material));
    }

    private unsafe void UpdateBuffers()
    {
        var view = renderContext!.Scene.Viewer!.GetView();
        var proj = renderContext.Scene.Viewer.GetProjection();
        var matSize = (nuint) sizeof(Matrix4X4<float>);
        
        uniformBuffer!.UpdateData(0, matSize, (float*)&view);
        uniformBuffer.UpdateData((nint)matSize, matSize, (float*)&proj);
    }

    private unsafe void RenderMaterialGroup(IEnumerable<IRenderedObject> objects, IGlMaterial material)
    {
        tracer.PutFrame(new RenderTraceFrame($"Material: {material.MaterialId}"));
        
        material.Use();
        
        foreach (var obj in objects)
        {
            tracer.PutFrame(new RenderTraceFrame($"RenderObject: {obj.ObjectId}"));
            if (obj is GlRenderObject o)
            {
                material.ApplyUniform(o.TransformUniform);
            }

            buffers!.UpdateGeometry(obj.Mesh);
            buffers.Bind();
            
            gl!.DrawElements(PrimitiveType.Triangles, (uint) obj.Mesh.Indices.Length, DrawElementsType.UnsignedInt, null);
            tracer.TryWithdrawFrame();
        }
        
        tracer.TryWithdrawFrame();
    }
    
    #endregion

    private void ValidateRenderState()
    {
        if (buffers == null)
        {
            throw new Exception("Render state is invalid: buffers were not properly initialized.");
        }

        if (renderContext == null || gl == null)
        {
            throw new Exception("Render state is invalid: render contexts were not specified.");
        }
    }

    #region Mutation

    /// <summary>
    /// Inject a new material instance into the renderer using the provided definition.
    /// </summary>
    /// <param name="definition">The material definition to use for material instantiation.</param>
    public void InjectMaterial(IGlMaterialDefinition definition)
    {
        Enqueue(() =>
        {
            if (materials.TryGetValue(definition.MaterialId, out var mat))
            {
                mat.UpdateDefinition(definition);
            }
            else
            {
                materials[definition.MaterialId] = new GlMaterial(gl!, definition);
            }
        });
    }
    
    public override void UpdateShaders(IEnumerable<IShaderDefinition> shaders)
    {
        throw new NotImplementedException();
    }
    
    #endregion

    private void OnTraceException(RenderTraceException exception)
    {
        throw new Exception($"{exception}{Environment.NewLine}Inner exception: {exception.InnerException}");
    }

    public void Dispose()
    {
        buffers?.Dispose();
        uniformBuffer?.Dispose();
        fallbackMaterial?.Dispose();
        materials.Values.DisposeAll();
        disposables.Dispose();
    }
}