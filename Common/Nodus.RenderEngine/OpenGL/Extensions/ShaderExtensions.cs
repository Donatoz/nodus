using Nodus.RenderEngine.Common;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

public static class ShaderExtensions
{
    public static ShaderType ToShaderType(this ShaderSourceType sourceType)
    {
        return sourceType switch
        {
            ShaderSourceType.Fragment => ShaderType.FragmentShader,
            ShaderSourceType.Vertex => ShaderType.VertexShader,
            ShaderSourceType.Compute => ShaderType.ComputeShader,
            _ => throw new ArgumentException($"Failed to convert {sourceType} to shader type.")
        };
    }
}