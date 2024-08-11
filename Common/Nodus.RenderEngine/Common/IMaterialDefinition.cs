namespace Nodus.RenderEngine.Common;

public interface IMaterialDefinition
{
    string MaterialId { get; }
    IReadOnlyCollection<IShaderDefinition> Shaders { get; }
    IList<IShaderUniform> Uniforms { get; }
    uint RenderPriority { get; }
}