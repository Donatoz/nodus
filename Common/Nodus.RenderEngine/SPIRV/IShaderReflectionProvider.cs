using Silk.NET.SPIRV.Reflect;

namespace Nodus.RenderEngine.SPIRV;

public interface IShaderReflectionProvider
{
    Reflect Api { get; }
}

public sealed class ShaderReflectionProvider(Reflect api) : IShaderReflectionProvider
{
    public Reflect Api { get; } = api;
}