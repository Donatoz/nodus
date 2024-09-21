namespace Nodus.RenderEngine.Common;

public interface IMaterialDefinition
{
    string MaterialId { get; }
    uint RenderPriority { get; }
    IReadOnlyCollection<IShaderDefinition> Shaders { get; }
}