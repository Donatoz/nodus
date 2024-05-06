using Nodus.RenderEngine.Common;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

public interface IGlBuffer<T> : IUnmanagedHook where T : unmanaged
{
    void Bind();
    void UpdateData(Span<T> data);
}

public class GlBuffer<T> : GlObject, IGlBuffer<T> where T : unmanaged
{
    private readonly BufferTargetARB bufferType;

    public unsafe GlBuffer(GL gl, Span<T> data, BufferTargetARB bufferType, bool autoUnbind = true) : base(gl)
    {
        this.bufferType = bufferType;
        
        gl.IterateErrors();
        
        Handle = Context.GenBuffer();
        Bind();
        
        gl.TryThrowNextError();
        
        UpdateData(data);
        
        gl.TryThrowNextError();

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
    }

    public void Dispose()
    {
        Context.DeleteBuffer(Handle);
    }
}