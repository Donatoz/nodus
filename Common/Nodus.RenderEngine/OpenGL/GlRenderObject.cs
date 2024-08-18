using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.OpenGL.Convention;

namespace Nodus.RenderEngine.OpenGL;

public class GlRenderObject : RenderedObject
{
    public IGlShaderUniform TransformUniform { get; }
    
    public GlRenderObject(IGeometryPrimitive geometry, ITransform transform, string materialId) : base(geometry, transform, materialId)
    {
        TransformUniform =
            new GlMatrix4Uniform(UniformConvention.TransformUniformName, GetEffectiveTransform);
    }
}