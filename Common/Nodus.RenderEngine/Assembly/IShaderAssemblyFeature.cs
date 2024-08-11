using Nodus.RenderEngine.Common;

namespace Nodus.RenderEngine.Assembly;

public interface IShaderAssemblyFeature
{
    ushort AssemblyPriority { get; init; }
}

public interface IShaderAssemblyContext
{
    IReadOnlyCollection<IShaderAssemblyFeature> EffectiveFeatures { get; }
}

public readonly struct ShaderAssemblyContext : IShaderAssemblyContext
{
    public IReadOnlyCollection<IShaderAssemblyFeature> EffectiveFeatures { get; }
    
    public ShaderAssemblyContext(IReadOnlyCollection<IShaderAssemblyFeature> effectiveFeatures)
    {
        EffectiveFeatures = effectiveFeatures;
    }

}