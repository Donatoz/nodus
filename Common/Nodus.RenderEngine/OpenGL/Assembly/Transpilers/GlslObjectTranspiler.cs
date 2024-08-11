using System.Globalization;
using System.Numerics;
using Nodus.RenderEngine.Assembly;

namespace Nodus.RenderEngine.OpenGL.Assembly;

public static class GlslObjectTranspiler
{
    public static ShaderObjectDefinition ObjectToSource(object value)
    {
        return value switch
        {
            Vector4 v4 => new ShaderObjectDefinition("vec4", false, $"{v4.X.ToString("0.000", CultureInfo.InvariantCulture)}," +
                                                                    $"{v4.Y.ToString("0.000", CultureInfo.InvariantCulture)}," +
                                                                    $"{v4.Z.ToString("0.000", CultureInfo.InvariantCulture)}," +
                                                                    $"{v4.W.ToString("0.000", CultureInfo.InvariantCulture)}"),
            float f => new ShaderObjectDefinition("float", true, f.ToString("0.000", CultureInfo.InvariantCulture)),
            _ => throw new ArgumentException($"Failed to transpile object ({value}) to GLSL source: object type not supported.")
        };
    }
}