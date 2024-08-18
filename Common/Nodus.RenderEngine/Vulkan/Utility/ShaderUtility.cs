using Nodus.RenderEngine.Common;
using Silk.NET.Vulkan;

namespace Nodus.RenderEngine.Vulkan.Utility;

public static class ShaderUtility
{
    public static ShaderStageFlags SourceTypeToStage(ShaderSourceType sourceType) => sourceType switch
    {
        ShaderSourceType.Vertex => ShaderStageFlags.VertexBit,
        ShaderSourceType.Fragment => ShaderStageFlags.FragmentBit,
        ShaderSourceType.Compute => ShaderStageFlags.ComputeBit,
        _ => ShaderStageFlags.None
    };
}