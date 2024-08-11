using Nodus.Core.Extensions;
using Nodus.RenderEngine.Assembly;

namespace Nodus.RenderEngine.OpenGL.Assembly;

public record GlVersionFeature(ushort Version, GlVersionFeature.GlShaderVersionType Type) : IShaderAssemblyFeature
{
    public ushort AssemblyPriority { get; init; } = 0;
    
    public string VersionTypeToString()
    {
        return Type == GlShaderVersionType.Core ? "core" : "es";
    }

    public enum GlShaderVersionType
    {
        Core,
        Es
    }
}