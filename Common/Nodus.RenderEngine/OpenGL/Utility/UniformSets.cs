using System.Diagnostics;

namespace Nodus.RenderEngine.OpenGL;

public static class UniformSets
{
    private static readonly Stopwatch ShaderTime;

    static UniformSets()
    {
        ShaderTime = new Stopwatch();
        ShaderTime.Start();
    }
    
    public static readonly IEnumerable<IGlShaderUniform> TimerUniform = new[]
    {
        new GlFloatUniform("time", () => ShaderTime.ElapsedMilliseconds / 100f, true)
    };
}