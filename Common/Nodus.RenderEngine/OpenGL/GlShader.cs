using Nodus.RenderEngine.Common;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

/// <summary>
/// Represents an OpenGL shader object.
/// </summary>
public interface IGlShader : IUnmanagedHook
{
    /// <summary>
    /// Compiles the shader.
    /// </summary>
    void Compile();
}

public class GlShader : GlObject, IGlShader
{
    private IShaderSource source = null!;
    
    public GlShader(GL context, IShaderSource source, ShaderType type) : base(context)
    {
        Handle = Context.CreateShader(type);
        
        UpdateSource(source);
    }

    public void UpdateSource(IShaderSource src)
    {
        source = src;
        Context.ShaderSource(Handle, source.FetchSource());
        Compile();
    }
    
    public void Compile()
    {
        Context.CompileShader(Handle);
        Context.TryThrowShaderError(Handle, source.ToString());
    }
    
    public void Dispose()
    {
        Context.DeleteShader(Handle);
    }

    public override string? ToString()
    {
        return source.ToString();
    }
}