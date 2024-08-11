using Nodus.RenderEngine.Common;

namespace Nodus.RenderEngine.Assembly;

public interface IShaderAssembler
{
    IShaderAssembly AssembleShader(IShaderFeatureTranspiler transpiler);
}

public class ModularShaderAssembler : IShaderAssembler
{
    private readonly List<IShaderAssemblyFeature> features;
    
    public ModularShaderAssembler()
    {
        features = new List<IShaderAssemblyFeature>();
    }

    public void ClearFeatures() => features.Clear();

    public void AddFeature(IShaderAssemblyFeature feature) => features.Add(feature);

    public void RemoveFeature(IShaderAssemblyFeature feature) => features.Remove(feature);
    
    public IShaderAssembly AssembleShader(IShaderFeatureTranspiler transpiler)
    {
        var context = new ShaderAssemblyContext(features);
        
        return new ShaderAssembly(features
            .OrderBy(x => x.AssemblyPriority)
            .Select(x => transpiler.CreateToken(x, context)));
    }
}