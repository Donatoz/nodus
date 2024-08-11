using Nodus.RenderEngine.Common;
using Silk.NET.OpenGL;

namespace Nodus.RenderEngine.OpenGL;

/// <summary>
/// Represents an OpenGL vertex array object.
/// </summary>
public interface IGlVertexArray : IUnmanagedHook
{
    /// <summary>
    /// Binds the vertex array object for rendering.
    /// </summary>
    void Bind();

    /// <summary>
    /// Sets the vertex attribute for the vertex array object.
    /// </summary>
    /// <param name="index">The index of the vertex attribute.</param>
    /// <param name="byteCount">The number of bytes for the vertex attribute.</param>
    /// <param name="type">The data type of the vertex attribute.</param>
    /// <param name="vertexSize">The size of each vertex in the vertex array.</param>
    /// <param name="offSet">The offset of the vertex attribute within each vertex.</param>
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
        
        Context.TryThrowNextError($"Failed to set vertex attribute: Type={type}, Idx={index}");
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