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
    
    public static IEnumerable<IGlShaderUniform> GetTimerUniform(Stopwatch? timer = null) => new[]
    {
        new GlFloatUniform("time", () => (timer ?? ShaderTime).ElapsedMilliseconds / 100f % short.MaxValue, true)
    };
}