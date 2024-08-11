using Nodus.RenderEngine.Common;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;


/// <summary>
/// Represents a generic OpenGL buffer for holding the data of a specific type.
/// </summary>
/// <typeparam name="T">The type of data stored in the buffer.</typeparam>
public interface IGlBuffer<T> : IUnmanagedHook where T : unmanaged
{
    /// <summary>
    /// Bind the buffer.
    /// </summary>
    void Bind();

    /// <summary>
    /// Update the data of the buffer.
    /// </summary>
    /// <param name="data">The new data to update the buffer with.</param>
    void UpdateData(Span<T> data);
}

public class GlBuffer<T> : GlObject, IGlBuffer<T> where T : unmanaged
{
    private readonly BufferTargetARB bufferType;

    public GlBuffer(GL gl, Span<T> data, BufferTargetARB bufferType, bool autoUnbind = true) : base(gl)
    {
        this.bufferType = bufferType;
        
        Handle = Context.GenBuffer();
        Bind();
        
        UpdateData(data);
        
        gl.TryThrowAllErrors();

        if (autoUnbind)
        {
            Context.BindBuffer(bufferType, 0);
        }
    }

    public void Bind()
    {
        Context.BindBuffer(bufferType, Handle);
    }

    public unsafe void UpdateData(Span<T> data)
    {
        fixed (void* d = data)
        {
            Context.BufferData(bufferType, (nuint) (data.Length * sizeof(T)), d, BufferUsageARB.StaticDraw);
        }
        
        Context.TryThrowNextError($"Failed to update buffer data. {this}, Data={data.Length}");
    }

    public void Dispose()
    {
        Context.DeleteBuffer(Handle);
    }

    public override string ToString()
    {
        return $"[Buffer={bufferType}, Type={typeof(T)}]";
    }
}