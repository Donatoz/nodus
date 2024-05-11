using Nodus.RenderEngine.Common;

namespace Nodus.RenderEngine.Assembly;

public interface IShaderAssembler
{
    IShaderAssembly AssembleShader(ShaderSourceType type);
}