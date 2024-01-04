using System;

namespace Nodus.Core.Effects;

public interface IShaderUniform
{
    string UniformName { get; }
    Func<float[]> UniformValueGetter { get; }
}

public readonly struct ConstantUniform : IShaderUniform
{
    public string UniformName { get; }
    public Func<float[]> UniformValueGetter { get; }
    
    public ConstantUniform(string uniformName, float[] value)
    {
        UniformName = uniformName;
        UniformValueGetter = () => value;
    }
}