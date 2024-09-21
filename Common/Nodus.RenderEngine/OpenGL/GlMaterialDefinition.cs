using Nodus.RenderEngine.Common;

namespace Nodus.RenderEngine.OpenGL;

public interface IGlMaterialDefinition : IMaterialDefinition
{
    IList<IShaderUniform> Uniforms { get; }
    IReadOnlyCollection<IGlTexture> Textures { get; set; }
}

public class GlMaterialDefinition : IGlMaterialDefinition
{
    public string MaterialId { get; }
    public IReadOnlyCollection<IShaderDefinition> Shaders { get; }
    public IReadOnlyCollection<IGlTexture> Textures { get; set; }
    public IList<IShaderUniform> Uniforms { get; }
    public uint RenderPriority { get; }

    public GlMaterialDefinition(IEnumerable<IShaderDefinition> shaders, IEnumerable<IGlShaderUniform> uniforms, IEnumerable<IGlTexture> textures, uint priority = 0)
    {
        MaterialId = Guid.NewGuid().ToString();
        Shaders = shaders.ToArray();
        Uniforms = uniforms.OfType<IShaderUniform>().ToArray();
        Textures = textures.ToArray();
        RenderPriority = priority;
    }
}