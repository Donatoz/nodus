using System.Diagnostics;
using System.Text;
using Nodus.Core.Extensions;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

/// <summary>
/// Represents an OpenGL shader program.
/// </summary>
public class GlShaderProgram : GlObject, IDisposable
{
    private readonly GL gl;
    private readonly string[] shaders;
    
    public GlShaderProgram(GL gl, params IGlShader[] attachedShaders) : base(gl)
    {
        this.gl = gl;
        Handle = Context.CreateProgram();

        attachedShaders.ForEach(x => Context.AttachShader(Handle, x.Handle));
        
        Context.LinkProgram(Handle);
        Context.GetProgram(Handle, GLEnum.LinkStatus, out var status);
        
        if (status == 0)
        {
            throw new Exception($"Failed to link the program: {Context.GetProgramInfoLog(Handle)}" +
                                $"{Environment.NewLine}{GetShadersTraceList()}");
        }

        shaders = attachedShaders.Select(x => x.ToString()).OfType<string>().ToArray();
        
        attachedShaders.ForEach(x => Context.DetachShader(Handle, x.Handle));
        
        gl.TryThrowNextError($"Failed to create shader program.{Environment.NewLine}{GetShadersTraceList()}");
    }

    /// <summary>
    /// Retrieve and log information about active uniforms in the shader program.
    /// </summary>
    /// <remarks>
    /// This method retrieves the number of active uniforms in the shader program and logs
    /// information about each uniform, including its name, size, and type.
    /// </remarks>
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
        gl.TryThrowNextError();
    }

    /// <summary>
    /// Apply a uniform value to the shader program.
    /// </summary>
    /// <param name="uniform">The shader uniform to apply.</param>
    public void ApplyUniform(IGlShaderUniform uniform)
    {
        var location = Context.GetUniformLocation(Handle, uniform.Name);
        
        if (location == -1)
        {
            if (uniform.Optional) return;
            
            throw new Exception($"{uniform.Name} uniform not found on shader.");
        }
        
        uniform.Apply(gl, location);

        gl.TryThrowNextError($"Failed to apply uniform: {uniform}.{Environment.NewLine}{GetShadersTraceList()}");
    }

    private string GetShadersTraceList()
    {
        return $"Shaders:{Environment.NewLine}" +
               $"{string.Join(Environment.NewLine, shaders.Select(x => $"[{x}]"))}";
    }

    public void Dispose()
    {
        Context.DeleteProgram(Handle);
    }
}