using Nodus.RenderEngine.Assembly;

namespace Nodus.RenderEngine.OpenGL.Assembly;

public interface IGlShaderAssemblyFeature : IShaderAssemblyFeature
{
    uint AssemblyPriority { get; }
}