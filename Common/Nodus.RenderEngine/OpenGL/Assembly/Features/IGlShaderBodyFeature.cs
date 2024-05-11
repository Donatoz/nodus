using Nodus.RenderEngine.Assembly;

namespace Nodus.RenderEngine.OpenGL.Assembly;

public interface IGlShaderBodyFeature : IGlShaderAssemblyFeature
{
    string BodyName { get; }
}

public interface IGlShaderBodyToken
{
    void AlterBody(IShaderAssemblyContext context);
}