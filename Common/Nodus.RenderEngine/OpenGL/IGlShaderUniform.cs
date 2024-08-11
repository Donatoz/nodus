using System.Numerics;
using Nodus.RenderEngine.Common;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

public interface IGlShaderUniform : IShaderUniform
{
    bool Optional { get; }
    
    void Apply(GL context, int location);
}

public abstract record GlUniformBase<T> : IGlShaderUniform
{
    public string Name { get; }
    public bool Optional { get; }

    protected Func<T> Getter { get; }

    protected GlUniformBase(string name, Func<T> getter, bool optional = false)
    {
        Name = name;
        Getter = getter;
        Optional = optional;
    }

    public abstract void Apply(GL context, int location);
}

public record GlFloatUniform : GlUniformBase<float>
{
    public GlFloatUniform(string name, Func<float> getter, bool optional = false) : base(name, getter, optional)
    {
    }

    public override void Apply(GL context, int location)
    {
        context.Uniform1(location, Getter.Invoke());
    }
}

public record GlIntUniform : GlUniformBase<int>
{
    public GlIntUniform(string name, Func<int> getter, bool optional = false) : base(name, getter, optional)
    {
    }

    public override void Apply(GL context, int location)
    {
        context.Uniform1(location, Getter.Invoke());
    }
}

public record GlMatrix4Uniform : GlUniformBase<Matrix4X4<float>>
{
    public GlMatrix4Uniform(string name, Func<Matrix4X4<float>> getter, bool optional = false) : base(name, getter, optional)
    {
    }

    public override unsafe void Apply(GL context, int location)
    {
        var mat = Getter.Invoke();
        context.UniformMatrix4(location, 1, false, (float*)&mat);
    }
}