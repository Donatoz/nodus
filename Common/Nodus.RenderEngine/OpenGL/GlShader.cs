using Nodus.RenderEngine.Common;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

public interface IGlShader : IUnmanagedHook
{
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
}