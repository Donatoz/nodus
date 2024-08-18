using Nodus.RenderEngine.Common;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

/// <summary>
/// Represents an OpenGL buffer object that holds fragmented data of different types.
/// </summary>
public unsafe interface IGlDivBuffer : IUnmanagedHook<uint>
{
    /// <summary>
    /// Bind the buffer to the current OpenGL context.
    /// </summary>
    void Bind();

    /// <summary>
    /// Allocate a memory of the specified size for the buffer.
    /// </summary>
    /// <param name="size">The size of the buffer to allocate.</param>
    void Allocate(nuint size);

    /// <summary>
    /// Update the data of the buffer object.
    /// </summary>
    /// <param name="offset">The offset, in bytes, from the beginning of the buffer object where the data will be updated.</param>
    /// <param name="size">The size, in bytes, of the data to update.</param>
    /// <param name="data">A pointer to the data to update.</param>
    void UpdateData(nint offset, nuint size, void* data);
}

public class GlDivBuffer : GlObject, IGlDivBuffer
{
    private readonly BufferTargetARB bufferType;
    
    public GlDivBuffer(GL gl, BufferTargetARB bufferType, nuint allocSize, bool autoUnbind = true, IRenderTracer? tracer = null) : base(gl, tracer)
    {
        this.bufferType = bufferType;

        Handle = Context.GenBuffer();
        
        Bind();
        Allocate(allocSize);

        if (autoUnbind)
        {
            Context.BindBuffer(this.bufferType, 0);
        }
    }

    public void Bind()
    {
        Context.BindBuffer(bufferType, Handle);
    }

    public unsafe void Allocate(nuint size)
    {
        Context.BufferData(bufferType, size, null, GLEnum.StaticDraw);
        TryThrowTracedGlError($"{GetType()}:Allocate", $"Failed to allocate buffer data. Type={bufferType}, Size={size}");
    }

    public unsafe void UpdateData(nint offset, nuint size, void* data)
    {
        Context.BufferSubData(bufferType, offset, size, data);
        TryThrowTracedGlError($"{GetType()}:UpdateData", $"Failed to update buffer data. Type={bufferType}, Size={size}");
    }
    
    public void Dispose()
    {
        Context.DeleteBuffer(Handle);
    }
}