using Nodus.RenderEngine.Vulkan.Sync;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Nodus.RenderEngine.Vulkan.Memory;

public interface IVkBuffer : IVkUnmanagedHook
{
    Buffer WrappedBuffer { get; }
    ulong Size { get; }

    void MapToHost();
    void Unmap();
    void UpdateData<T>(Span<T> data, ulong offset) where T : unmanaged;
}

public interface IVkBufferContext
{
    ulong Size { get; }
    BufferUsageFlags Usage { get; }
    SharingMode SharingMode { get; }
    IVkFence[]? BlockingFences { get; }
}

public record VkBufferContext(
    ulong Size, 
    BufferUsageFlags Usage, 
    SharingMode SharingMode, 
    IVkFence[]? BlockingFences = null) 
    : IVkBufferContext;