using Nodus.RenderEngine.Common;
using Nodus.RenderEngine.OpenGL;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine;

public static class GLExtensions
{
    public static void IterateErrors(this GL gl, Action<GLEnum>? onError = null)
    {
        GLEnum error;
        do
        {
            error = gl.GetError();
            if (error != GLEnum.Zero)
            {
                onError?.Invoke(error);
            }
        } while (error != GLEnum.NoError);
    }

    public static void TryThrowNextError(this GL gl, string? message = null)
    {
#if DEBUG
        var error = gl.GetError();

        if (error != GLEnum.NoError)
        {
            throw new OpenGlException($"{error.ToString()}. {message}");
        }
#endif
    }

    public static void TryThrowAllErrors(this GL gl)
    {
        gl.IterateErrors(x => throw new Exception($"Caught OpenGL exception: {x}"));
    }

    public static void TryThrowShaderError(this GL gl, uint shaderHandle, string? shaderName = null)
    {
        var log = gl.GetShaderInfoLog(shaderHandle);

        if (!string.IsNullOrWhiteSpace(log))
        {
            throw new OpenGlException($"Failed to compile shader ({shaderName ?? string.Empty}): {log}");
        }
    }
}