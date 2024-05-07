using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

public interface IGlShaderUniform
{
    string Name { get; }
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