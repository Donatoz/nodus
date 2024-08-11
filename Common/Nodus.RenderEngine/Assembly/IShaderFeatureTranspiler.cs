namespace Nodus.RenderEngine.Assembly;

public interface IShaderFeatureTranspiler
{
    IShaderAssemblyToken CreateToken(IShaderAssemblyFeature feature, IShaderAssemblyContext context);
}