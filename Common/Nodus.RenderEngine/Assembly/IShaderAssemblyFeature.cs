using Nodus.RenderEngine.Common;

namespace Nodus.RenderEngine.Assembly;

public interface IShaderAssemblyFeature
{
    IShaderAssemblyToken GetToken(IShaderAssemblyContext context);
}

public interface IShaderAssemblyContext
{
    IReadOnlyCollection<IShaderAssemblyFeature> EffectiveFeatures { get; }
}