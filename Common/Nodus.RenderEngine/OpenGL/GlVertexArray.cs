using Nodus.RenderEngine.Common;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

public interface IGlVertexArray : IUnmanagedHook
{
    void Bind();
    void SetVertexAttribute(uint index, int byteCount, VertexAttribPointerType type, uint vertexSize, int offSet);
}

public class GlVertexArray<TVert> : GlObject, IGlVertexArray where TVert : unmanaged
{
    public GlVertexArray(GL gl) : base(gl)
    {
        Handle = Context.GenVertexArray();
    }

    public unsafe void SetVertexAttribute(uint index, int byteCount, VertexAttribPointerType type, uint vertexSize, int offSet)
    {
        Context.VertexAttribPointer(index, byteCount, type, false, vertexSize * (uint) sizeof(TVert), (void*) (offSet * sizeof(TVert)));
        Context.EnableVertexAttribArray(index);
    }

    public void Bind()
    {
        Context.BindVertexArray(Handle);
    }

    public void Dispose()
    {
        Context.DeleteVertexArray(Handle);
    }
}