using System.Diagnostics;
using System.Text;
using Nodus.Core.Extensions;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

public class GlShaderProgram : GlObject, IDisposable
{
    private readonly GL gl;
    
    public GlShaderProgram(GL gl, params IGlShader[] attachedShaders) : base(gl)
    {
        this.gl = gl;
        Handle = Context.CreateProgram();

        attachedShaders.ForEach(x => Context.AttachShader(Handle, x.Handle));
        
        Context.LinkProgram(Handle);
        Context.GetProgram(Handle, GLEnum.LinkStatus, out var status);
        
        if (status == 0)
        {
            throw new Exception($"Program failed to link with error: {Context.GetProgramInfoLog(Handle)}");
        }
        
        attachedShaders.ForEach(x => Context.DetachShader(Handle, x.Handle));
    }

    public void DebugUniforms()
    {
        gl.GetProgram(Handle, GLEnum.ActiveUniforms, out var activeUniforms);
        
        for (var i = 0; i < activeUniforms; i++)
        {
            var name = GetActiveUniform((uint)i, out var size, out var type);
            Trace.WriteLine($"Uniform {i} - Name: {name} - Size: {size} - Type: {type}");
        }
    }

    private string GetActiveUniform(uint index, out int size, out UniformType type)
    {
        gl.GetActiveUniform(Handle, index, out size, out type);

        var uniformName = new Span<byte>();

        gl.GetActiveUniformName(Handle, index, out var length, uniformName);

        return Encoding.UTF8.GetString(uniformName);
    }

    public void Use()
    {
        Context.UseProgram(Handle);
    }

    public void ApplyUniform(IGlShaderUniform uniform)
    {
        var location = Context.GetUniformLocation(Handle, uniform.Name);
        
        if (location == -1)
        {
            if (uniform.Optional) return;
            
            throw new Exception($"{uniform.Name} uniform not found on shader.");
        }
        
        uniform.Apply(gl, location);
    }

    public void Dispose()
    {
        Context.DeleteProgram(Handle);
    }
}