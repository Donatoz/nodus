using System.Diagnostics;
using Nodus.Core.Extensions;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

public class GlShaderProgram : GlObject, IDisposable
{
    public GlShaderProgram(GL gl, params IGlShader[] attachedShaders) : base(gl)
    {
        Handle = Context.CreateProgram();

        attachedShaders.ForEach(x => Context.AttachShader(Handle, x.Handle));
        
        Context.LinkProgram(Handle);
        Context.GetProgram(Handle, GLEnum.LinkStatus, out var status);
        
        if (status == 0)
        {
            throw new Exception($"Program failed to link with error: {Context.GetProgramInfoLog(Handle)}");
        }
        
        attachedShaders.ForEach(x => Context.DetachShader(Handle, x.Handle));
        
        Trace.WriteLine("Program linked");
    }

    public void Use()
    {
        Context.UseProgram(Handle);
    }

    public void SetUniform(string name, float value)
    {
        var location = Context.GetUniformLocation(Handle, name);
        
        if (location == -1)
        {
            throw new Exception($"{name} uniform not found on shader.");
        }
        
        Context.Uniform1(location, value);
    }

    public void Dispose()
    {
        Context.DeleteProgram(Handle);
    }
}